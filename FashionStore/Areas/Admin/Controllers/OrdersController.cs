// File: OrdersController.cs
using ClosedXML.Excel;
namespace FashionStore.Areas.Admin.Controllers;

public class OrdersController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IOrderService _orders;
    public OrdersController(ApplicationDbContext db, IOrderService orders) { _db = db; _orders = orders; }

    public async Task<IActionResult> Index(string? keyword, OrderStatus? status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        const int pageSize = 10;
        var q = _db.Orders.Include(o => o.Payments).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(o => o.OrderCode.Contains(k) || o.ReceiverName.Contains(k) || o.ReceiverPhone.Contains(k));
        }
        if (status.HasValue) q = q.Where(o => o.Status == status);
        if (fromDate.HasValue) q = q.Where(o => o.CreatedAt >= fromDate.Value.Date);
        if (toDate.HasValue) q = q.Where(o => o.CreatedAt < toDate.Value.Date.AddDays(1));

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        ViewBag.Paging = new PagingInfo { Page = page, TotalPages = totalPages };
        ViewBag.Status = status; ViewBag.Keyword = keyword; ViewBag.FromDate = fromDate; ViewBag.ToDate = toDate;
        ViewBag.StatusCounts = await _db.Orders.GroupBy(o => o.Status)
            .Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C);
        return View(await q.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Details).ThenInclude(d => d.ProductVariant).ThenInclude(v => v!.Product)
            .Include(o => o.StatusHistories).ThenInclude(h => h.ChangedByUser)
            .Include(o => o.Payments).ThenInclude(p => p.PaymentMethod)
            .Include(o => o.ShippingMethod)
            .AsSplitQuery().FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? note)
    {
        var (ok, error) = await _orders.UpdateStatusAsync(id, status, User.GetUserId(), note);
        if (ok) TempData["Success"] = $"Đã chuyển đơn sang \"{status.ToLabel()}\""; else TempData["Error"] = error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayment(int id, int paymentId, PaymentStatus status)
    {
        var pay = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.OrderId == id);
        if (pay == null) return NotFound();
        pay.Status = status;
        pay.PaidAt = status == PaymentStatus.Paid ? DateTime.Now : null;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thanh toán";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Export(string? keyword, OrderStatus? status, DateTime? fromDate, DateTime? toDate)
    {
        var q = _db.Orders.Include(o => o.Payments).ThenInclude(p => p.PaymentMethod).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(o => o.OrderCode.Contains(k) || o.ReceiverName.Contains(k) || o.ReceiverPhone.Contains(k));
        }
        if (status.HasValue) q = q.Where(o => o.Status == status);
        if (fromDate.HasValue) q = q.Where(o => o.CreatedAt >= fromDate.Value.Date);
        if (toDate.HasValue) q = q.Where(o => o.CreatedAt < toDate.Value.Date.AddDays(1));
        var orders = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Đơn hàng");
        string[] heads = { "Mã đơn", "Ngày đặt", "Người nhận", "SĐT", "Địa chỉ", "Tạm tính", "Phí vận chuyển", "Giảm giá", "Tổng tiền", "Trạng thái", "Phương thức TT", "Thanh toán" };
        for (int c = 0; c < heads.Length; c++) ws.Cell(1, c + 1).Value = heads[c];

        for (int i = 0; i < orders.Count; i++)
        {
            var o = orders[i]; var r = i + 2; var pay = o.Payments.FirstOrDefault();
            ws.Cell(r, 1).Value = o.OrderCode;
            ws.Cell(r, 2).Value = o.CreatedAt;
            ws.Cell(r, 3).Value = o.ReceiverName;
            ws.Cell(r, 4).Value = o.ReceiverPhone;
            ws.Cell(r, 5).Value = o.ShippingAddress;
            ws.Cell(r, 6).Value = o.SubTotal;
            ws.Cell(r, 7).Value = o.ShippingFee;
            ws.Cell(r, 8).Value = o.DiscountAmount;
            ws.Cell(r, 9).Value = o.TotalAmount;
            ws.Cell(r, 10).Value = o.Status.ToLabel();
            ws.Cell(r, 11).Value = pay?.PaymentMethod?.Name ?? "";
            ws.Cell(r, 12).Value = pay?.Status.ToLabel() ?? "";
        }
        ws.Column(2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        ws.Range(2, 6, orders.Count + 1, 9).Style.NumberFormat.Format = "#,##0";
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        return ExcelFile(wb, $"don-hang-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    private FileContentResult ExcelFile(XLWorkbook wb, string fileName)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}