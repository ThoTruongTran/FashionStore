// File: Controllers/CartController.cs
namespace FashionStore.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cart;
    public CartController(ICartService cart) => _cart = cart;

    public async Task<IActionResult> Index() =>
        View(await _cart.GetCartAsync(User.GetUserId()) ?? new Cart());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int variantId, int quantity = 1, string? returnUrl = null)
    {
        var (ok, error) = await _cart.AddAsync(User.GetUserId(), variantId, quantity);
        if (!ok)
        {
            TempData["Error"] = error;
            return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
        TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int itemId, int quantity)
    {
        var (ok, error) = await _cart.UpdateAsync(User.GetUserId(), itemId, quantity);
        if (!ok) TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int itemId)
    {
        await _cart.RemoveAsync(User.GetUserId(), itemId);
        return RedirectToAction(nameof(Index));
    }
}