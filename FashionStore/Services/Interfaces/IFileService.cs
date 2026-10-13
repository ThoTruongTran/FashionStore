// File: Services/Interfaces/IFileService.cs
namespace FashionStore.Services.Interfaces;
public interface IFileService
{
    Task<string> SaveImageAsync(IFormFile file, string folder);   // trả về đường dẫn dạng /uploads/folder/xxx.jpg
    void Delete(string? url);
}