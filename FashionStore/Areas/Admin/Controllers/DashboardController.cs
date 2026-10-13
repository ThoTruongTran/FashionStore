// File: Areas/Admin/Controllers/DashboardController.cs   (trang tạm, Phần 3 sẽ làm đầy đủ)
namespace FashionStore.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin,Staff")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.Products = await _db.Products.CountAsync();
        ViewBag.Orders = await _db.Orders.CountAsync();
        ViewBag.Users = await _db.Users.CountAsync();
        return View();
    }
}