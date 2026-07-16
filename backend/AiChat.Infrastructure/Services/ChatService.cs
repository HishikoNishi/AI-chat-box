using System.Text.Json;
using AiChat.Application.Chat;
using AiChat.Application.Common;
using AiChat.Domain.Entities;
using AiChat.Domain.Enums;
using AiChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiChat.Infrastructure.Services;

public sealed class ChatService(AppDbContext dbContext, IOptions<UploadOptions> uploadOptions) : IChatService
{
    private readonly UploadOptions _uploadOptions = uploadOptions.Value;

    public async Task<ChatSessionResponse> CreateSessionAsync(Guid userId, string? title, CancellationToken cancellationToken = default)
    {
        var session = new ChatSession
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? "New chat" : title.Trim()[..Math.Min(title.Trim().Length, 200)]
        };
        dbContext.ChatSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    public async Task<IReadOnlyList<ChatSessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.ChatSessions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new ChatSessionResponse(x.Id, x.Title, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ChatSessions
            .Include(x => x.Messages)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Chat session was not found.");

        var attachments = await dbContext.Attachments
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        dbContext.Attachments.RemoveRange(attachments);
        dbContext.ChatSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var attachment in attachments)
            TryDeletePhysicalFile(attachment.StoragePath);

        TryDeleteSessionDirectory(userId, sessionId);
    }

    public async Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await RequireSessionAsync(userId, sessionId, cancellationToken);

        var messages = await dbContext.Messages.AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.SessionId,
                x.Role,
                x.Content,
                x.CreatedAt,
                Attachments = x.Attachments
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => new AttachmentResponse(
                        a.Id,
                        a.FileName,
                        a.ContentType,
                        a.SizeBytes,
                        BuildAttachmentUrl(a.Id)))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return messages
            .Select(x => new ChatMessageResponse(x.Id, x.SessionId, x.Role, x.Content, x.CreatedAt, x.Attachments))
            .ToList();
    }

    public async Task<ChatMessageResponse> AddMessageAsync(
        Guid userId,
        Guid sessionId,
        MessageRole role,
        string content,
        CancellationToken cancellationToken = default,
        Guid? presetId = null,
        IReadOnlyList<Guid>? attachmentIds = null)
    {
        var normalizedContent = content?.Trim() ?? string.Empty;
        var ids = attachmentIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];

        if (string.IsNullOrWhiteSpace(normalizedContent) && ids.Count == 0)
            throw new ValidationException("Message content cannot be empty.");

        if (normalizedContent.Length == 0)
            normalizedContent = "Đính kèm tệp";

        var session = await RequireSessionAsync(userId, sessionId, cancellationToken);
        var message = new ChatMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = normalizedContent
        };

        if (presetId is { } id)
            message.Id = id;

        session.UpdatedAt = DateTimeOffset.UtcNow;
        if (session.Title == "New chat" && role == MessageRole.User)
            session.Title = message.Content[..Math.Min(message.Content.Length, 60)];

        dbContext.Messages.Add(message);

        if (ids.Count > 0)
        {
            var pendingAttachments = await dbContext.Attachments
                .Where(x => ids.Contains(x.Id) && x.SessionId == sessionId && x.MessageId == null)
                .ToListAsync(cancellationToken);

            if (pendingAttachments.Count != ids.Count)
                throw new ValidationException("One or more attachments are invalid for this session.");

            foreach (var attachment in pendingAttachments)
                attachment.MessageId = message.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetMessageResponseAsync(message.Id, cancellationToken)
            ?? throw new NotFoundException("Message was not found after save.");
    }

    public async Task<AttachmentResponse> UploadAttachmentAsync(
        Guid userId,
        Guid sessionId,
        string fileName,
        string contentType,
        Stream content,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        await RequireSessionAsync(userId, sessionId, cancellationToken);
        ValidateUpload(fileName, contentType, sizeBytes);

        var safeFileName = Path.GetFileName(fileName);
        var attachmentId = Guid.NewGuid();
        var relativePath = Path.Combine(userId.ToString("N"), sessionId.ToString("N"), $"{attachmentId:N}_{safeFileName}");
        var absolutePath = GetAbsolutePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using (var fileStream = File.Create(absolutePath))
            await content.CopyToAsync(fileStream, cancellationToken);

        var attachment = new Attachment
        {
            Id = attachmentId,
            SessionId = sessionId,
            FileName = safeFileName,
            ContentType = contentType,
            StoragePath = relativePath.Replace('\\', '/'),
            SizeBytes = sizeBytes
        };

        dbContext.Attachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToAttachmentResponse(attachment);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> OpenAttachmentAsync(
        Guid userId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.Attachments.AsNoTracking()
            .Include(x => x.Session)
            .SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);

        if (attachment is null || attachment.Session.UserId != userId)
            return null;

        var absolutePath = GetAbsolutePath(attachment.StoragePath);
        if (!File.Exists(absolutePath))
            return null;

        var stream = File.OpenRead(absolutePath);
        return (stream, attachment.ContentType, attachment.FileName);
    }

    internal string GetAbsolutePath(string storagePath) =>
        Path.GetFullPath(Path.Combine(_uploadOptions.RootPath, storagePath.Replace('/', Path.DirectorySeparatorChar)));

    internal static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private async Task<ChatMessageResponse?> GetMessageResponseAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var message = await dbContext.Messages.AsNoTracking()
            .Where(x => x.Id == messageId)
            .Select(x => new
            {
                x.Id,
                x.SessionId,
                x.Role,
                x.Content,
                x.CreatedAt,
                Attachments = x.Attachments
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => new AttachmentResponse(
                        a.Id,
                        a.FileName,
                        a.ContentType,
                        a.SizeBytes,
                        BuildAttachmentUrl(a.Id)))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return message is null
            ? null
            : new ChatMessageResponse(message.Id, message.SessionId, message.Role, message.Content, message.CreatedAt, message.Attachments);
    }

    private async Task<ChatSession> RequireSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken) =>
        await dbContext.ChatSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Chat session was not found.");

    private void ValidateUpload(string fileName, string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ValidationException("File name is required.");

        if (sizeBytes <= 0)
            throw new ValidationException("File is empty.");

        if (sizeBytes > _uploadOptions.MaxFileSizeBytes)
            throw new ValidationException($"File exceeds the maximum size of {_uploadOptions.MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (!_uploadOptions.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException("This file type is not supported.");
    }

    private void TryDeletePhysicalFile(string storagePath)
    {
        try
        {
            var absolutePath = GetAbsolutePath(storagePath);
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void TryDeleteSessionDirectory(Guid userId, Guid sessionId)
    {
        try
        {
            var directory = Path.GetFullPath(Path.Combine(_uploadOptions.RootPath, userId.ToString("N"), sessionId.ToString("N")));
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static ChatSessionResponse ToResponse(ChatSession session) =>
        new(session.Id, session.Title, session.CreatedAt, session.UpdatedAt);

    private static AttachmentResponse ToAttachmentResponse(Attachment attachment) =>
        new(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, BuildAttachmentUrl(attachment.Id));

    private static string BuildAttachmentUrl(Guid attachmentId) =>
        $"/api/chat-sessions/attachments/{attachmentId}/file";
}
