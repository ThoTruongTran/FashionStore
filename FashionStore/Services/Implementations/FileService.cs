// File: Services/Implementations/FileService.cs
namespace FashionStore.Services.Implementations;

public class FileService : IFileService
{
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private readonly IWebHostEnvironment _env;
    public FileService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) throw new InvalidOperationException("Chỉ chấp nhận ảnh jpg, png, webp, gif");
        if (file.Length > 5 * 1024 * 1024) throw new InvalidOperationException("Mỗi ảnh tối đa 5MB");

        var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using var fs = new FileStream(Path.Combine(dir, name), FileMode.Create);
        await file.CopyToAsync(fs);
        return $"/uploads/{folder}/{name}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/")) return;   // chỉ xóa file do mình upload
        var path = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
    }
}