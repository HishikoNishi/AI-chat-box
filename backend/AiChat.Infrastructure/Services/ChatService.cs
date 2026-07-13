using AiChat.Application.Chat;
using AiChat.Application.Common;
using AiChat.Domain.Entities;
using AiChat.Domain.Enums;
using AiChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiChat.Infrastructure.Services;

public sealed class ChatService(AppDbContext dbContext) : IChatService
{
    public async Task<ChatSessionResponse> CreateSessionAsync(Guid userId, string? title, CancellationToken cancellationToken = default)
    {
        var session = new ChatSession { UserId = userId, Title = string.IsNullOrWhiteSpace(title) ? "New chat" : title.Trim()[..Math.Min(title.Trim().Length, 200)] };
        dbContext.ChatSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    public async Task<IReadOnlyList<ChatSessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.ChatSessions.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.UpdatedAt)
            .Select(x => new ChatSessionResponse(x.Id, x.Title, x.CreatedAt, x.UpdatedAt)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await RequireSessionAsync(userId, sessionId, cancellationToken);
        return await dbContext.Messages.AsNoTracking().Where(x => x.SessionId == sessionId).OrderBy(x => x.CreatedAt)
            .Select(x => new ChatMessageResponse(x.Id, x.Role, x.Content, x.CreatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<ChatMessageResponse> AddMessageAsync(Guid userId, Guid sessionId, MessageRole role, string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new ValidationException("Message content cannot be empty.");
        var session = await RequireSessionAsync(userId, sessionId, cancellationToken);
        var message = new ChatMessage { SessionId = sessionId, Role = role, Content = content.Trim() };
        session.UpdatedAt = DateTimeOffset.UtcNow;
        if (session.Title == "New chat" && role == MessageRole.User) session.Title = message.Content[..Math.Min(message.Content.Length, 60)];
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ChatMessageResponse(message.Id, message.Role, message.Content, message.CreatedAt);
    }

    private async Task<ChatSession> RequireSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken) =>
        await dbContext.ChatSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Chat session was not found.");

    private static ChatSessionResponse ToResponse(ChatSession session) => new(session.Id, session.Title, session.CreatedAt, session.UpdatedAt);
}
