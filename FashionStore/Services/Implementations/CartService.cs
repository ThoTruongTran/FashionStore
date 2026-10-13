// File: Services/Implementations/CartService.cs
namespace FashionStore.Services.Implementations;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;
    public CartService(ApplicationDbContext db) => _db = db;

    public async Task<Cart?> GetCartAsync(int userId) =>
        await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Product).ThenInclude(p => p!.Images)
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Color)
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Size)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<int> CountAsync(int userId) =>
        await _db.CartItems.Where(i => i.Cart!.UserId == userId).SumAsync(i => (int?)i.Quantity) ?? 0;

    public async Task<(bool Ok, string? Error)> AddAsync(int userId, int variantId, int quantity)
    {
        if (quantity < 1) return (false, "Số lượng không hợp lệ");
        var variant = await _db.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == variantId);
        if (variant?.Product is not { IsActive: true }) return (false, "Sản phẩm không tồn tại");

        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) { cart = new Cart { UserId = userId }; _db.Carts.Add(cart); }

        var item = cart.Items.FirstOrDefault(i => i.ProductVariantId == variantId);
        var newQty = (item?.Quantity ?? 0) + quantity;
        if (newQty > variant.StockQuantity) return (false, $"Chỉ còn {variant.StockQuantity} sản phẩm trong kho");

        if (item == null) cart.Items.Add(new CartItem { ProductVariantId = variantId, Quantity = newQty });
        else item.Quantity = newQty;
        cart.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(int userId, int itemId, int quantity)
    {
        var item = await _db.CartItems.Include(i => i.ProductVariant)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart!.UserId == userId);
        if (item == null) return (false, "Không tìm thấy sản phẩm trong giỏ");

        if (quantity < 1) _db.CartItems.Remove(item);
        else if (quantity > item.ProductVariant!.StockQuantity)
            return (false, $"Chỉ còn {item.ProductVariant.StockQuantity} sản phẩm trong kho");
        else item.Quantity = quantity;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task RemoveAsync(int userId, int itemId)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(i => i.Id == itemId && i.Cart!.UserId == userId);
        if (item == null) return;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
    }
}