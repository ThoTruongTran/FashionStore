namespace FashionStore.Services.Interfaces;
public interface ICartService
{
    Task<Cart?> GetCartAsync(int userId);
    Task<int> CountAsync(int userId);
    Task<(bool Ok, string? Error)> AddAsync(int userId, int variantId, int quantity);
    Task<(bool Ok, string? Error)> UpdateAsync(int userId, int itemId, int quantity);
    Task RemoveAsync(int userId, int itemId);
}