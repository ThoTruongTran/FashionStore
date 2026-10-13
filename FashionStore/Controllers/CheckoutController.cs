using System.Net;
namespace FashionStore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly ICouponService _coupons;
    private readonly IVnPayService _vnpay;
    private readonly IEmailService _email;

    public CheckoutController(ApplicationDbContext db, ICartService cart, IOrderService orders,
        ICouponService coupons, IVnPayService vnpay, IEmailService email)
    { _db = db; _cart = cart; _orders = orders; _coupons = coupons; _vnpay = vnpay; _email = email; }

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

        var method = await _db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == vm.PaymentMethodId && p.IsActive);
        var isVnPay = method != null && method.Name.Contains("VNPay");
        if (isVnPay && !_vnpay.IsConfigured)
            ModelState.AddModelError("", "Thanh toán VNPay hiện chưa được cấu hình");

        if (!ModelState.IsValid) { await LoadAsync(vm, cart); return View(vm); }

        var (ok, error, order) = await _orders.PlaceOrderAsync(userId, vm);
        if (!ok)
        {
            ModelState.AddModelError("", error!);
            await LoadAsync(vm, cart);
            return View(vm);
        }

        await SendConfirmationAsync(order!);

        return isVnPay
            ? RedirectToAction("Pay", "Payment", new { orderId = order!.Id })
            : RedirectToAction(nameof(Success), new { code = order!.OrderCode });
    }

    // AJAX: kiểm tra mã giảm giá
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string? code)
    {
        var cart = await _cart.GetCartAsync(User.GetUserId());
        if (cart == null || !cart.Items.Any()) return Json(new { ok = false, message = "Giỏ hàng trống", discount = 0 });

        var r = await _coupons.ValidateAsync(code ?? "", cart.Total);
        return Json(new
        {
            ok = r.Ok,
            message = r.Ok ? $"Áp dụng thành công, giảm {r.Discount.ToVnd()}" : r.Error,
            discount = r.Ok ? (long)r.Discount : 0L
        });
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
        var methods = await _db.PaymentMethods.Where(p => p.IsActive).ToListAsync();
        vm.PaymentMethods = methods.Where(p => _vnpay.IsConfigured || !p.Name.Contains("VNPay")).ToList();
        if (vm.ShippingMethodId == 0) vm.ShippingMethodId = vm.ShippingMethods.FirstOrDefault()?.Id ?? 0;
        if (vm.PaymentMethodId == 0) vm.PaymentMethodId = vm.PaymentMethods.FirstOrDefault()?.Id ?? 0;
    }

    private async Task SendConfirmationAsync(Order order)
    {
        var to = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(to)) return;

        var rows = string.Concat(order.Details.Select(d =>
            $"<tr><td style='padding:4px 8px'>{WebUtility.HtmlEncode(d.ProductName)}</td>" +
            $"<td align='center'>{d.Quantity}</td><td align='right' style='padding:4px 8px'>{(d.Quantity * d.UnitPrice).ToVnd()}</td></tr>"));
        var body = $@"<h2>Cảm ơn bạn đã đặt hàng tại FashionStore!</h2>
<p>Mã đơn hàng: <b>{order.OrderCode}</b></p>
<table border='1' cellspacing='0' style='border-collapse:collapse'>{rows}</table>
<p>Tạm tính: {order.SubTotal.ToVnd()}<br/>Phí vận chuyển: {order.ShippingFee.ToVnd()}<br/>
Giảm giá: -{order.DiscountAmount.ToVnd()}<br/><b>Tổng thanh toán: {order.TotalAmount.ToVnd()}</b></p>
<p>Giao tới: {WebUtility.HtmlEncode(order.ReceiverName)} - {WebUtility.HtmlEncode(order.ReceiverPhone)}<br/>{WebUtility.HtmlEncode(order.ShippingAddress)}</p>";
        await _email.SendAsync(to, $"Xác nhận đơn hàng {order.OrderCode}", body);
    }
}