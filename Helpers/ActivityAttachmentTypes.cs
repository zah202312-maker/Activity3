namespace ActivityApp.Helpers;

public static class ActivityAttachmentTypes
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".mov"] = "video/quicktime",
        [".ogv"] = "video/ogg",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    private static readonly HashSet<string> PreviewableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm", ".mov", ".ogv"
    };

    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm", ".mov", ".ogv"
    };

    private static readonly HashSet<string> ReportExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".xls", ".xlsx"
    };

    public static bool TryGetContentType(string fileName, out string contentType) =>
        ContentTypes.TryGetValue(Path.GetExtension(fileName), out contentType!);

    public static bool IsActivityMedia(string fileName) =>
        MediaExtensions.Contains(Path.GetExtension(fileName));

    public static bool IsActivityReport(string fileName) =>
        ReportExtensions.Contains(Path.GetExtension(fileName));

    public static bool TryGetPreviewContentType(string fileName, out string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (PreviewableExtensions.Contains(extension))
            return ContentTypes.TryGetValue(extension, out contentType!);

        contentType = "";
        return false;
    }
}
