namespace MangoTaika.Services;

public interface IFileUploadService
{
    Task<string> SaveFileAsync(IFormFile file, string subfolder);
    bool IsValidImage(IFormFile file);
    bool IsValidMedia(IFormFile file);
}

public class FileUploadService(IWebHostEnvironment env) : IFileUploadService
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly HashSet<string> VideoExtensions = [".mp4", ".webm", ".ogg", ".mov"];
    private const long MaxImageSize = 5 * 1024 * 1024;  // 5 Mo
    private const long MaxVideoSize = 50 * 1024 * 1024;  // 50 Mo

    // Extensions dangereuses interdites (exécutables, scripts, etc.)
    private static readonly HashSet<string> DangerousExtensions =
    [
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".msi", ".com", ".scr",
        ".js", ".vbs", ".wsf", ".hta", ".cpl", ".inf", ".reg", ".rgs",
        ".php", ".asp", ".aspx", ".jsp", ".py", ".rb", ".pl", ".cgi",
        ".html", ".htm", ".svg", ".xml", ".xhtml", ".shtml",
        ".jar", ".war", ".class", ".config", ".cshtml", ".razor"
    ];

    // Signatures magiques (magic bytes) pour valider le contenu réel
    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        [".jpg"] = [new byte[] { 0xFF, 0xD8, 0xFF }],
        [".jpeg"] = [new byte[] { 0xFF, 0xD8, 0xFF }],
        [".png"] = [new byte[] { 0x89, 0x50, 0x4E, 0x47 }],
        [".gif"] = [new byte[] { 0x47, 0x49, 0x46, 0x38 }],
        [".webp"] = [new byte[] { 0x52, 0x49, 0x46, 0x46 }],
        [".mp4"] = [new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0x66, 0x74, 0x79, 0x70 }],
    };

    public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
    {
        // Nettoyer le nom du sous-dossier (empêcher path traversal)
        subfolder = Path.GetFileName(subfolder);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Bloquer les extensions dangereuses
        if (DangerousExtensions.Contains(ext))
            throw new InvalidOperationException("Type de fichier non autorisé.");

        // Vérifier les magic bytes si on a une signature connue
        if (!await ValidateMagicBytesAsync(file, ext))
            throw new InvalidOperationException("Le contenu du fichier ne correspond pas à son extension.");

        var dir = Path.Combine(env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(dir);

        // Nom de fichier aléatoire (pas de nom original = pas de path traversal)
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{subfolder}/{fileName}";
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxImageSize) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ImageExtensions.Contains(ext) && !DangerousExtensions.Contains(ext);
    }

    public bool IsValidMedia(IFormFile file)
    {
        if (file.Length == 0) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (DangerousExtensions.Contains(ext)) return false;

        var isImage = ImageExtensions.Contains(ext);
        var isVideo = VideoExtensions.Contains(ext);
        if (!isImage && !isVideo) return false;

        var maxSize = isVideo ? MaxVideoSize : MaxImageSize;
        return file.Length <= maxSize;
    }

    private static async Task<bool> ValidateMagicBytesAsync(IFormFile file, string ext)
    {
        if (!MagicBytes.TryGetValue(ext, out var signatures))
            return true; // Pas de signature connue, on laisse passer

        using var reader = file.OpenReadStream();
        var headerBytes = new byte[8];
        var bytesRead = await reader.ReadAsync(headerBytes);
        if (bytesRead < 3) return false;

        return signatures.Any(sig =>
            sig.Length <= bytesRead &&
            headerBytes.AsSpan(0, sig.Length).SequenceEqual(sig));
    }
}
