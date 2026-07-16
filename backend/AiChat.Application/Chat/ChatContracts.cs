using AiChat.Domain.Enums;

namespace AiChat.Application.Chat;

public sealed record AttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Url);

public sealed record ChatSessionResponse(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid SessionId,
    MessageRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AttachmentResponse> Attachments);

public interface IChatService
{
    Task<ChatSessionResponse> CreateSessionAsync(Guid userId, string? title, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<ChatMessageResponse> AddMessageAsync(
        Guid userId,
        Guid sessionId,
        MessageRole role,
        string content,
        CancellationToken cancellationToken = default,
        Guid? presetId = null,
        IReadOnlyList<Guid>? attachmentIds = null);
    Task<AttachmentResponse> UploadAttachmentAsync(
        Guid userId,
        Guid sessionId,
        string fileName,
        string contentType,
        Stream content,
        long sizeBytes,
        CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType, string FileName)?> OpenAttachmentAsync(
        Guid userId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
