using System.Security.Claims;
using System.Text;
using AiChat.Application.Chat;
using AiChat.Application.Common;
using AiChat.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AiChat.Api.Hubs;

[Authorize]
public sealed class ChatHub(IAiChatService aiChatService, IChatService chatService, ILogger<ChatHub> logger) : Hub
{
    public async Task SendMessage(string sessionId, string text, string? clientTempId = null, string[]? attachmentIds = null)
    {
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Clients.Caller.SendAsync("ReceiveError", "Invalid chat session.");
            return;
        }

        try
        {
            var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var parsedAttachmentIds = ParseAttachmentIds(attachmentIds);

            var message = await chatService.AddMessageAsync(
                userId,
                parsedSessionId,
                MessageRole.User,
                text,
                Context.ConnectionAborted);

            await Clients.Caller.SendAsync("MessageSaved", new
            {
                message.Id,
                message.SessionId,
                message.Role,
                message.Content,
                message.CreatedAt,
                ClientTempId = clientTempId
            }, Context.ConnectionAborted);

            var history = await chatService.GetMessagesAsync(userId, parsedSessionId, Context.ConnectionAborted);

            var assistantMessageId = Guid.NewGuid();
            await Clients.Caller.SendAsync("MessageStarted", new
            {
                Id = assistantMessageId,
                SessionId = parsedSessionId,
                Role = MessageRole.Assistant,
                Content = "",
                CreatedAt = DateTime.UtcNow,
                Attachments = Array.Empty<object>()
            }, Context.ConnectionAborted);

            var assistantContent = new StringBuilder();
            await foreach (var token in aiChatService.GenerateContentStreamAsync(history, Context.ConnectionAborted))
            {
                assistantContent.Append(token);
                await Clients.Caller.SendAsync("ReceiveToken", token, Context.ConnectionAborted);
            }

            if (assistantContent.Length == 0)
                throw new AiProviderException("Gemini did not return any text for this request.");

            var assistantMessage = await chatService.AddMessageAsync(
                userId,
                parsedSessionId,
                MessageRole.Assistant,
                assistantContent.ToString(),
                Context.ConnectionAborted,
                presetId: assistantMessageId);

            await Clients.Caller.SendAsync("StreamComplete", assistantMessage, Context.ConnectionAborted);
        }
        catch (ValidationException exception) { await Clients.Caller.SendAsync("ReceiveError", exception.Message); }
        catch (NotFoundException exception) { await Clients.Caller.SendAsync("ReceiveError", exception.Message); }
        catch (RateLimitException)
        {
            await Clients.Caller.SendAsync("ReceiveError", "Gemini is rate-limited right now. Please wait a moment and try again.");
        }
        catch (AiProviderException exception)
        {
            logger.LogWarning(exception, "Gemini request failed for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("ReceiveError", "The AI response is unavailable right now. Please try again shortly.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error sending message for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("ReceiveError", "Unable to process this message. Please try again.");
        }
    }

    private static IReadOnlyList<Guid>? ParseAttachmentIds(string[]? attachmentIds)
    {
        if (attachmentIds is not { Length: > 0 }) return null;

        var parsed = new List<Guid>();
        foreach (var attachmentId in attachmentIds)
        {
            if (!Guid.TryParse(attachmentId, out var id))
                throw new ValidationException("One or more attachment ids are invalid.");
            parsed.Add(id);
        }

        return parsed;
    }
}
