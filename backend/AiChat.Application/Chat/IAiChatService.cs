namespace AiChat.Application.Chat;

/// <summary>Streams text deltas from the server-side AI provider.</summary>
public interface IAiChatService
{
    IAsyncEnumerable<string> GenerateContentStreamAsync(
        Guid userId,
        IReadOnlyList<ChatMessageResponse> messages,
        CancellationToken cancellationToken = default);
}
