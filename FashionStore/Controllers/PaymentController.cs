using System.Net;
namespace FashionStore.Controllers;

public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IVnPayService _vnpay;
    public PaymentController(ApplicationDbContext db, IVnPayService vnpay) { _db = db; _vnpay = vnpay; }

    // Chuyển khách sang cổng VNPay (cũng dùng cho "Thanh toán lại")
    [Authorize]
    public async Task<IActionResult> Pay(int orderId)
    {
        var userId = User.GetUserId();
        var order = await _db.Orders.Include(o => o.Payments).ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) return NotFound();

        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault(p => p.PaymentMethod!.Name.Contains("VNPay"));
        if (payment == null || payment.Status == PaymentStatus.Paid || order.Status != OrderStatus.Pending || !_vnpay.IsConfigured)
        {
            TempData["Error"] = "Đơn hàng này không thể thanh toán online";
            return RedirectToAction("Details", "Order", new { id = orderId });
        }
        if (payment.Status == PaymentStatus.Failed) { payment.Status = PaymentStatus.Unpaid; await _db.SaveChangesAsync(); }

        var addr = HttpContext.Connection.RemoteIpAddress;
        var ip = addr == null || IPAddress.IsLoopback(addr) ? "127.0.0.1"
               : addr.IsIPv4MappedToIPv6 ? addr.MapToIPv4().ToString() : addr.ToString();
        var returnUrl = Url.Action(nameof(VnPayReturn), "Payment", null, Request.Scheme)!;
        return Redirect(_vnpay.CreatePaymentUrl(order.OrderCode, payment.Amount, ip, returnUrl));
    }

    // VNPay đưa khách quay lại đây. Bảo mật dựa trên chữ ký, không dựa trên đăng nhập.
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn()
    {
        var r = _vnpay.ParseReturn(Request.Query);
        if (r == null || r.TxnRef.Length <= 6)
            return View("Result", new PaymentResultViewModel { Success = false, Message = "Dữ liệu trả về không hợp lệ (sai chữ ký)." });

        var orderCode = r.TxnRef[..^6];
        var order = await _db.Orders.Include(o => o.Payments).ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        var payment = order?.Payments.OrderByDescending(p => p.Id).FirstOrDefault(p => p.PaymentMethod!.Name.Contains("VNPay"));
        if (order == null || payment == null)
            return View("Result", new PaymentResultViewModel { Success = false, Message = "Không tìm thấy đơn hàng." });

        if (payment.Status != PaymentStatus.Paid && order.Status != OrderStatus.Cancelled)
        {
            if (r.Success && r.Amount == (long)payment.Amount)
            {
                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.Now;
                payment.TransactionCode = r.TransactionNo;
                order.StatusHistories.Add(new OrderStatusHistory { Status = order.Status, Note = $"Thanh toán VNPay thành công (GD {r.TransactionNo})" });
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.TransactionCode = r.TransactionNo;
            }
            await _db.SaveChangesAsync();
        }

        var paid = payment.Status == PaymentStatus.Paid;
        return View("Result", new PaymentResultViewModel
        {
            Success = paid,
            OrderCode = order.OrderCode,
            Message = paid ? "Thanh toán thành công. Cảm ơn bạn!"
                           : $"Thanh toán chưa thành công (mã {r.ResponseCode}). Bạn có thể thanh toán lại trong mục Đơn hàng của tôi."
        });
    }
}