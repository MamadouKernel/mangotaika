using MangoTaika.Services;

namespace MangoTaika.Helpers;

public static class ImageUploadExtensions
{
    public static async Task<string?> SaveImageAsync(
        this IFileUploadService fileUploadService,
        IFormFile? file,
        string? currentUrl,
        string subfolder,
        string invalidImageMessage)
    {
        if (file is null || file.Length == 0)
        {
            return string.IsNullOrWhiteSpace(currentUrl) ? null : currentUrl;
        }

        if (!fileUploadService.IsValidImage(file))
        {
            throw new InvalidOperationException(invalidImageMessage);
        }

        return await fileUploadService.SaveFileAsync(file, subfolder);
    }
}
