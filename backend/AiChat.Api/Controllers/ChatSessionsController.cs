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

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await chatService.DeleteSessionAsync(GetUserId(), sessionId, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{sessionId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<ChatMessageResponse>>> GetMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        try { return Ok(await chatService.GetMessagesAsync(GetUserId(), sessionId, cancellationToken)); }
        catch (NotFoundException exception) { return NotFound(new { message = exception.Message }); }
    }

    [HttpPost("{sessionId:guid}/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<AttachmentResponse>> UploadAttachment(Guid sessionId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "File is empty." });

        try
        {
            await using var stream = file.OpenReadStream();
            var attachment = await chatService.UploadAttachmentAsync(
                GetUserId(),
                sessionId,
                file.FileName,
                file.ContentType,
                stream,
                file.Length,
                cancellationToken);
            return Ok(attachment);
        }
        catch (NotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (ValidationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("attachments/{attachmentId:guid}/file")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId, CancellationToken cancellationToken)
    {
        var opened = await chatService.OpenAttachmentAsync(GetUserId(), attachmentId, cancellationToken);
        if (opened is null) return NotFound();

        return File(opened.Value.Stream, opened.Value.ContentType, opened.Value.FileName);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
