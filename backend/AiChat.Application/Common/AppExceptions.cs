namespace AiChat.Application.Common;

public sealed class ValidationException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class NotFoundException(string message) : Exception(message);
public sealed class AiProviderException(string message) : Exception(message);
public sealed class RateLimitException(string message) : Exception(message);
