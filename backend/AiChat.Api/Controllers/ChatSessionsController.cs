using System.Security.Claims;
using AiChat.Api.Contracts;
using AiChat.Application.Chat;
using AiChat.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiChat.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chat-sessions")]
public sealed class ChatSessionsController(IChatService chatService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChatSessionResponse>>> GetSessions(CancellationToken cancellationToken) =>
        Ok(await chatService.GetSessionsAsync(GetUserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ChatSessionResponse>> CreateSession(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await chatService.CreateSessionAsync(GetUserId(), request.Title, cancellationToken);
        return CreatedAtAction(nameof(GetMessages), new { sessionId = session.Id }, session);
    }

    [HttpGet("{sessionId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<ChatMessageResponse>>> GetMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        try { return Ok(await chatService.GetMessagesAsync(GetUserId(), sessionId, cancellationToken)); }
        catch (NotFoundException exception) { return NotFound(new { message = exception.Message }); }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
