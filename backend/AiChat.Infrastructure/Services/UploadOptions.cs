namespace AiChat.Infrastructure.Services;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    public string RootPath { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "application/pdf",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];
}
