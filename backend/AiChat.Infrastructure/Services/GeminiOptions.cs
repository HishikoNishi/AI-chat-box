namespace AiChat.Infrastructure.Services;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gemini-2.5-flash-lite";
    public int MaxOutputTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 0.7;
    public int MaxRetries { get; init; } = 3;
}
