using FluentAssertions;
using MangoTaika.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class FileUploadServiceTests
{
    [Fact]
    public async Task SaveFileAsync_Saves_Valid_Png_Image()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new FileUploadService(new TestWebHostEnvironment(root));
            await using var stream = new MemoryStream(CreatePngBytes());
            IFormFile file = new FormFile(stream, 0, stream.Length, "Logo", "logo.png");

            var url = await service.SaveFileAsync(file, "groupes");

            url.Should().StartWith("/uploads/groupes/");
            var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            File.Exists(Path.Combine(root, relativePath)).Should().BeTrue();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task SaveFileAsync_Rejects_Image_With_Invalid_Magic_Bytes()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new FileUploadService(new TestWebHostEnvironment(root));
            await using var stream = new MemoryStream([0x00, 0x11, 0x22, 0x33, 0x44]);
            IFormFile file = new FormFile(stream, 0, stream.Length, "Logo", "logo.png");

            Func<Task> act = () => service.SaveFileAsync(file, "branches");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ne correspond pas*");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static byte[] CreatePngBytes()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52
        ];
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "MangoTaika.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class TestWebHostEnvironment(string rootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MangoTaika.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(rootPath);
        public string WebRootPath { get; set; } = rootPath;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = rootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(rootPath);
    }
}
