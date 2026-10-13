// File: Controllers/CheckoutController.cs
namespace FashionStore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    public CheckoutController(ApplicationDbContext db, ICartService cart, IOrderService orders)
    { _db = db; _cart = cart; _orders = orders; }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        var cart = await _cart.GetCartAsync(userId);
        if (cart == null || !cart.Items.Any())
        {
            TempData["Error"] = "Giỏ hàng đang trống";
            return RedirectToAction("Index", "Cart");
        }
        var user = await _db.Users.FindAsync(userId);
        var addr = await _db.UserAddresses.Where(a => a.UserId == userId).OrderByDescending(a => a.IsDefault).FirstOrDefaultAsync();
        var vm = new CheckoutViewModel
        {
            ReceiverName = addr?.ReceiverName ?? user!.FullName,
            ReceiverPhone = addr?.Phone ?? user!.PhoneNumber ?? "",
            Province = addr?.Province ?? "",
            District = addr?.District ?? "",
            Ward = addr?.Ward ?? "",
            StreetDetail = addr?.StreetDetail ?? "",
            SaveAddress = addr == null
        };
        await LoadAsync(vm, cart);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel vm)
    {
        var userId = User.GetUserId();
        var cart = await _cart.GetCartAsync(userId);
        if (cart == null || !cart.Items.Any()) return RedirectToAction("Index", "Cart");

        if (!ModelState.IsValid) { await LoadAsync(vm, cart); return View(vm); }

        var (ok, error, order) = await _orders.PlaceOrderAsync(userId, vm);
        if (!ok)
        {
            ModelState.AddModelError("", error!);
            await LoadAsync(vm, cart);
            return View(vm);
        }
        return RedirectToAction(nameof(Success), new { code = order!.OrderCode });
    }

    public async Task<IActionResult> Success(string code)
    {
        var userId = User.GetUserId();
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderCode == code && o.UserId == userId);
        return order == null ? NotFound() : View(order);
    }

    private async Task LoadAsync(CheckoutViewModel vm, Cart cart)
    {
        vm.Cart = cart;
        vm.ShippingMethods = await _db.ShippingMethods.Where(s => s.IsActive).ToListAsync();
        vm.PaymentMethods = await _db.PaymentMethods.Where(p => p.IsActive).ToListAsync();
        if (vm.ShippingMethodId == 0) vm.ShippingMethodId = vm.ShippingMethods.FirstOrDefault()?.Id ?? 0;
        if (vm.PaymentMethodId == 0) vm.PaymentMethodId = vm.PaymentMethods.FirstOrDefault()?.Id ?? 0;
    }
}