namespace ApparelShop.Services;

public static class FileUploadValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public static (bool IsValid, string? Error) Validate(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "No file uploaded.");

        if (file.Length > MaxFileSize)
            return (false, $"File size exceeds the 5MB limit.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return (false, $"File type '{ext}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return (false, $"MIME type '{file.ContentType}' is not allowed.");

        if (ext.Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = reader.ReadToEnd();
            if (content.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("onload=", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("onerror=", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "SVG files cannot contain scripts or event handlers.");
            }
        }

        return (true, null);
    }
}
