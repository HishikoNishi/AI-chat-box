namespace AiChat.Api.Contracts;

public sealed record CredentialsRequest(string Email, string Password);
public sealed record CreateSessionRequest(string? Title);
