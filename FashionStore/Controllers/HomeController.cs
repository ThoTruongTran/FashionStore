// File: Controllers/HomeController.cs
using System.Diagnostics;
using FashionStore.Models;
namespace FashionStore.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            Banners = await _db.Banners.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ToListAsync(),
            Categories = await _db.Categories.Where(c => c.IsActive && c.ParentCategoryId == null).ToListAsync(),
            NewProducts = await _db.Products.Include(p => p.Images).Include(p => p.Brand)
                .Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt).Take(8).ToListAsync(),
            SaleProducts = await _db.Products.Include(p => p.Images).Include(p => p.Brand)
                .Where(p => p.IsActive && p.DiscountPercent > 0).OrderByDescending(p => p.DiscountPercent).Take(4).ToListAsync()
        };
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}