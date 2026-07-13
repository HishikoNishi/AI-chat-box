using System.IdentityModel.Tokens.Jwt;
using AiChat.Domain.Entities;
using AiChat.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace AiChat.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_includes_authenticated_user_identity()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "test-secret-that-is-longer-than-thirty-two-characters",
            AccessTokenMinutes = 15
        }));
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };

        var response = service.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.Email, response.User.Email);
        Assert.True(response.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
    }
}
