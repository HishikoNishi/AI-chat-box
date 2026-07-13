namespace AiChat.Application.Chat;

/// <summary>Streams text deltas from the server-side AI provider.</summary>
public interface IAiChatService
{
    IAsyncEnumerable<string> GenerateContentStreamAsync(
        IReadOnlyList<ChatMessageResponse> messages,
        CancellationToken cancellationToken = default);
}
