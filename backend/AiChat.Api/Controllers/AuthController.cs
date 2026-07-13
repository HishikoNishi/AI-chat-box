using AiChat.Api.Contracts;
using AiChat.Application.Auth;
using AiChat.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AiChat.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(CredentialsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await authService.RegisterAsync(new RegisterRequest(request.Email, request.Password), cancellationToken);
            SetRefreshCookie(session);
            return Ok(session.Response);
        }
        catch (ConflictException exception) { return Conflict(new { message = exception.Message }); }
        catch (ValidationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(CredentialsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await authService.LoginAsync(new LoginRequest(request.Email, request.Password), cancellationToken);
            SetRefreshCookie(session);
            return Ok(session.Response);
        }
        catch (ValidationException exception) { return Unauthorized(new { message = exception.Message }); }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken)) return Unauthorized();
        var session = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (session is null) { DeleteRefreshCookie(); return Unauthorized(); }
        SetRefreshCookie(session);
        return Ok(session.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue("refresh_token", out var refreshToken)) await authService.RevokeAsync(refreshToken, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(RefreshSession session)
    {
        var isLocal = HttpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        Response.Cookies.Append("refresh_token", session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isLocal,
            // Deployed frontend and API have different origins; cross-site refresh needs None + Secure.
            SameSite = isLocal ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = session.RefreshTokenExpiresAt,
            Path = "/api/auth"
        });
    }

    private void DeleteRefreshCookie() => Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
}
