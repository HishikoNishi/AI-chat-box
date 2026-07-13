using AiChat.Application.Auth;
using AiChat.Application.Common;
using AiChat.Domain.Entities;
using AiChat.Infrastructure.Auth;
using AiChat.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiChat.Infrastructure.Services;

public sealed class AuthService(AppDbContext dbContext, IJwtTokenService tokens, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<RefreshSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new ConflictException("This email address is already registered.");

        var user = new User { Email = email };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);
        return await IssueSessionAsync(user, cancellationToken);
    }

    public async Task<RefreshSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new ValidationException("Invalid email or password.");
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new ValidationException("Invalid email or password.");

        return await IssueSessionAsync(user, cancellationToken);
    }

    public async Task<RefreshSession?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = tokens.HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (storedToken is null || !storedToken.IsActive) return null;

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        var session = await IssueSessionAsync(storedToken.User, cancellationToken, saveChanges: false);
        storedToken.ReplacedByTokenHash = tokens.HashToken(session.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = tokens.HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (storedToken is null || storedToken.RevokedAt is not null) return;
        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<RefreshSession> IssueSessionAsync(User user, CancellationToken cancellationToken, bool saveChanges = true)
    {
        var rawToken = tokens.CreateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);
        dbContext.RefreshTokens.Add(new RefreshToken { User = user, TokenHash = tokens.HashToken(rawToken), ExpiresAt = expiresAt });
        if (saveChanges) await dbContext.SaveChangesAsync(cancellationToken);
        return new RefreshSession(tokens.CreateAccessToken(user), rawToken, expiresAt);
    }

    private static string NormalizeEmail(string email)
    {
        if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var parsed)) throw new ValidationException("A valid email address is required.");
        return parsed.Address.ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 8) throw new ValidationException("Password must contain at least 8 characters.");
    }
}
