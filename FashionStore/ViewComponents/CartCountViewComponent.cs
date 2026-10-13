namespace FashionStore.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly ICartService _cart;
    public CartCountViewComponent(ICartService cart) => _cart = cart;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = 0;
        if (UserClaimsPrincipal.Identity?.IsAuthenticated == true)
            count = await _cart.CountAsync(UserClaimsPrincipal.GetUserId());
        return View(count);
    }
}