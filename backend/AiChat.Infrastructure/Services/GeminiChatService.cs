using System.Net;
using System.Text;
using System.Text.Json;
using AiChat.Application.Chat;
using AiChat.Application.Common;
using AiChat.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AiChat.Infrastructure.Services;

public sealed class GeminiChatService(HttpClient httpClient, IOptions<GeminiOptions> options, IChatService chatService) : IAiChatService
{
    private readonly GeminiOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async IAsyncEnumerable<string> GenerateContentStreamAsync(
        Guid userId,
        IReadOnlyList<ChatMessageResponse> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new AiProviderException("AI is not configured yet. Set the Gemini API key on the server.");
        if (messages.Count == 0)
            throw new AiProviderException("No messages were provided to Gemini.");

        using var response = await SendWithRetryAsync(userId, messages, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
            var payload = line[5..].Trim();
            if (payload.Length == 0 || payload == "[DONE]") continue;

            var text = ExtractText(payload);
            if (!string.IsNullOrEmpty(text)) yield return text;
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Guid userId, IReadOnlyList<ChatMessageResponse> messages, CancellationToken cancellationToken)
    {
        var maxRetries = Math.Clamp(_options.MaxRetries, 0, 5);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var request = await CreateRequestAsync(userId, messages, cancellationToken);
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.IsSuccessStatusCode) return response;

                var statusCode = response.StatusCode;
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                response.Dispose();

                if (IsTransient(statusCode) && attempt < maxRetries)
                {
                    await DelayForRetryAsync(attempt, cancellationToken);
                    continue;
                }

                if (statusCode == HttpStatusCode.TooManyRequests)
                    throw new RateLimitException("Gemini rate limit was reached.");
                throw new AiProviderException($"Gemini request failed ({(int)statusCode}). {errorMessage}");
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                await DelayForRetryAsync(attempt, cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw new AiProviderException("Gemini is temporarily unavailable. Please try again shortly.");
            }
        }
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(Guid userId, IReadOnlyList<ChatMessageResponse> messages, CancellationToken cancellationToken)
    {
        var contents = new List<object>();
        foreach (var message in messages)
        {
            var parts = new List<object> { new { text = message.Content } };

            if (message.Role == MessageRole.User)
            {
                foreach (var attachment in message.Attachments)
                {
                    if (!IsImageContentType(attachment.ContentType))
                    {
                        parts.Add(new { text = $"[Đính kèm: {attachment.FileName} ({attachment.ContentType})]" });
                        continue;
                    }

                    var opened = await chatService.OpenAttachmentAsync(userId, attachment.Id, cancellationToken);
                    if (opened is null)
                    {
                        parts.Add(new { text = $"[Không đọc được ảnh: {attachment.FileName}]" });
                        continue;
                    }

                    await using var stream = opened.Value.Stream;
                    using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory, cancellationToken);
                    var base64 = Convert.ToBase64String(memory.ToArray());
                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = attachment.ContentType,
                            data = base64
                        }
                    });
                }
            }

            contents.Add(new
            {
                role = message.Role == MessageRole.Assistant ? "model" : "user",
                parts
            });
        }

        var body = new
        {
            contents,
            generationConfig = new
            {
                temperature = _options.Temperature,
                maxOutputTokens = _options.MaxOutputTokens
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"v1beta/models/{Uri.EscapeDataString(_options.Model)}:streamGenerateContent?alt=sse")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _options.ApiKey);
        return request;
    }

    private static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        statusCode == HttpStatusCode.ServiceUnavailable ||
        statusCode == HttpStatusCode.GatewayTimeout ||
        (int)statusCode >= 500;

    private static Task DelayForRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var milliseconds = Math.Min(8_000, 1_000 * (1 << attempt)) + Random.Shared.Next(0, 500);
        return Task.Delay(milliseconds, cancellationToken);
    }

    private static string? ExtractText(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return null;
        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) return null;

        var builder = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
            if (part.TryGetProperty("text", out var text)) builder.Append(text.GetString());
        return builder.ToString();
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(raw);
            return document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message)
                ? message.GetString() ?? "Unknown Gemini error."
                : "Unknown Gemini error.";
        }
        catch (JsonException) { return "Unknown Gemini error."; }
    }
}
