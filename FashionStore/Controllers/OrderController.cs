// File: Controllers/OrderController.cs
namespace FashionStore.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IOrderService _orders;
    public OrderController(ApplicationDbContext db, IOrderService orders) { _db = db; _orders = orders; }

    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 10;
        var userId = User.GetUserId();
        var query = _db.Orders.Where(o => o.UserId == userId);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        ViewBag.Paging = new PagingInfo { Page = page, TotalPages = totalPages };
        var orders = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.GetUserId();
        var order = await _db.Orders
            .Include(o => o.Details).ThenInclude(d => d.ProductVariant).ThenInclude(v => v!.Product)
            .Include(o => o.StatusHistories)
            .Include(o => o.Payments).ThenInclude(p => p.PaymentMethod)
            .Include(o => o.ShippingMethod)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (ok, error) = await _orders.CancelAsync(User.GetUserId(), id);
        if (ok) TempData["Success"] = "Đã hủy đơn hàng"; else TempData["Error"] = error;
        return RedirectToAction(nameof(Details), new { id });
    }
}