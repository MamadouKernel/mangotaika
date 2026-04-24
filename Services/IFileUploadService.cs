namespace MangoTaika.Services;

public interface IFileUploadService
{
    Task<string> SaveFileAsync(IFormFile file, string subfolder);
    Task<string> SaveImageAsync(IFormFile file, string subfolder);
    Task<string> SaveMediaAsync(IFormFile file, string subfolder);
    Task<string> SaveDocumentAsync(IFormFile file, string subfolder, IEnumerable<string>? allowedExtensions = null, long? maxSize = null);
    bool IsValidImage(IFormFile file);
    bool IsValidMedia(IFormFile file);
    bool IsValidDocument(IFormFile file, IEnumerable<string>? allowedExtensions = null, long? maxSize = null);
}

public class FileUploadService(IWebHostEnvironment env) : IFileUploadService
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly HashSet<string> VideoExtensions = [".mp4", ".webm", ".ogg", ".mov"];
    private static readonly HashSet<string> DefaultDocumentExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"];
    private const long MaxImageSize = 5 * 1024 * 1024;
    private const long MaxVideoSize = 50 * 1024 * 1024;
    private const long MaxDocumentSize = 15 * 1024 * 1024;

    private static readonly HashSet<string> DangerousExtensions =
    [
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".msi", ".com", ".scr",
        ".js", ".vbs", ".wsf", ".hta", ".cpl", ".inf", ".reg", ".rgs",
        ".php", ".asp", ".aspx", ".jsp", ".py", ".rb", ".pl", ".cgi",
        ".html", ".htm", ".svg", ".xml", ".xhtml", ".shtml",
        ".jar", ".war", ".class", ".config", ".cshtml", ".razor"
    ];

    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        [".jpg"] = [new byte[] { 0xFF, 0xD8, 0xFF }],
        [".jpeg"] = [new byte[] { 0xFF, 0xD8, 0xFF }],
        [".png"] = [new byte[] { 0x89, 0x50, 0x4E, 0x47 }],
        [".gif"] = [new byte[] { 0x47, 0x49, 0x46, 0x38 }],
        [".webp"] = [new byte[] { 0x52, 0x49, 0x46, 0x46 }],
        [".mp4"] = [new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0x66, 0x74, 0x79, 0x70 }],
        [".pdf"] = [new byte[] { 0x25, 0x50, 0x44, 0x46 }],
        [".doc"] = [new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }],
        [".xls"] = [new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }, new byte[] { 0x50, 0x4B, 0x03, 0x04 }],
        [".docx"] = [new byte[] { 0x50, 0x4B, 0x03, 0x04 }],
        [".xlsx"] = [new byte[] { 0x50, 0x4B, 0x03, 0x04 }]
    };

    public Task<string> SaveFileAsync(IFormFile file, string subfolder)
        => SaveDocumentAsync(file, subfolder);

    public Task<string> SaveImageAsync(IFormFile file, string subfolder)
        => SaveFileCoreAsync(file, subfolder, ImageExtensions, MaxImageSize);

    public Task<string> SaveMediaAsync(IFormFile file, string subfolder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var maxSize = VideoExtensions.Contains(ext) ? MaxVideoSize : MaxImageSize;
        return SaveFileCoreAsync(file, subfolder, ImageExtensions.Concat(VideoExtensions), maxSize);
    }

    public Task<string> SaveDocumentAsync(IFormFile file, string subfolder, IEnumerable<string>? allowedExtensions = null, long? maxSize = null)
        => SaveFileCoreAsync(file, subfolder, allowedExtensions ?? DefaultDocumentExtensions, maxSize ?? MaxDocumentSize);

    public bool IsValidImage(IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxImageSize)
            return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ImageExtensions.Contains(ext) && !DangerousExtensions.Contains(ext);
    }

    public bool IsValidMedia(IFormFile file)
    {
        if (file.Length == 0)
            return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (DangerousExtensions.Contains(ext))
            return false;

        var isImage = ImageExtensions.Contains(ext);
        var isVideo = VideoExtensions.Contains(ext);
        if (!isImage && !isVideo)
            return false;

        var maxSize = isVideo ? MaxVideoSize : MaxImageSize;
        return file.Length <= maxSize;
    }

    public bool IsValidDocument(IFormFile file, IEnumerable<string>? allowedExtensions = null, long? maxSize = null)
    {
        if (file.Length == 0)
            return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var normalizedAllowed = NormalizeAllowedExtensions(allowedExtensions ?? DefaultDocumentExtensions);
        if (!normalizedAllowed.Contains(ext) || DangerousExtensions.Contains(ext))
            return false;

        return file.Length <= (maxSize ?? MaxDocumentSize);
    }

    private async Task<string> SaveFileCoreAsync(IFormFile file, string subfolder, IEnumerable<string> allowedExtensions, long maxSize)
    {
        if (file.Length == 0 || file.Length > maxSize)
            throw new InvalidOperationException("Le fichier depasse la taille autorisee.");

        subfolder = Path.GetFileName(subfolder);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var normalizedAllowed = NormalizeAllowedExtensions(allowedExtensions);
        if (!normalizedAllowed.Contains(ext) || DangerousExtensions.Contains(ext))
            throw new InvalidOperationException("Type de fichier non autorise.");

        if (!await ValidateMagicBytesAsync(file, ext))
            throw new InvalidOperationException("Le contenu du fichier ne correspond pas a son extension.");

        var dir = Path.Combine(env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{subfolder}/{fileName}";
    }

    private static HashSet<string> NormalizeAllowedExtensions(IEnumerable<string> allowedExtensions)
    {
        return allowedExtensions
            .Select(ext => ext.StartsWith('.') ? ext.ToLowerInvariant() : $".{ext.ToLowerInvariant()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<bool> ValidateMagicBytesAsync(IFormFile file, string ext)
    {
        if (!MagicBytes.TryGetValue(ext, out var signatures))
            return true;

        using var reader = file.OpenReadStream();
        var headerBytes = new byte[16];
        var bytesRead = await reader.ReadAsync(headerBytes);
        if (bytesRead < 3)
            return false;

        return signatures.Any(sig =>
            sig.Length <= bytesRead &&
            headerBytes.AsSpan(0, sig.Length).SequenceEqual(sig));
    }
}
