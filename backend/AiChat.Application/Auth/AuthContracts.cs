namespace AiChat.Application.Auth;

public sealed record RegisterRequest(string? Email, string Password);
public sealed record LoginRequest(string? Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserResponse User);
public sealed record UserResponse(Guid Id, string Email);

public sealed record RefreshSession(AuthResponse Response, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

public interface IAuthService
{
    Task<RefreshSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<RefreshSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<RefreshSession?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
