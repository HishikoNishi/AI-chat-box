using AiChat.Domain.Enums;

namespace AiChat.Application.Chat;

public sealed record ChatSessionResponse(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record ChatMessageResponse(Guid Id, MessageRole Role, string Content, DateTimeOffset CreatedAt);

public interface IChatService
{
    Task<ChatSessionResponse> CreateSessionAsync(Guid userId, string? title, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<ChatMessageResponse> AddMessageAsync(Guid userId, Guid sessionId, MessageRole role, string content, CancellationToken cancellationToken = default);
}
