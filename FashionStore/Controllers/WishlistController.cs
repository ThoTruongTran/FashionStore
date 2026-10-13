namespace FashionStore.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _db;
    public WishlistController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var uid = User.GetUserId();
        var items = await _db.Wishlists
            .Include(w => w.Product).ThenInclude(p => p!.Images)
            .Include(w => w.Product).ThenInclude(p => p!.Brand)
            .Where(w => w.UserId == uid && w.Product!.IsActive)
            .OrderByDescending(w => w.AddedAt).ToListAsync();
        return View(items.Select(w => w.Product!).ToList());
    }

    // Danh sách Id đã thích, để JS tô đỏ các nút tim
    [HttpGet]
    public async Task<IActionResult> Ids() =>
        Json(await _db.Wishlists.Where(w => w.UserId == User.GetUserId()).Select(w => w.ProductId).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        var uid = User.GetUserId();
        var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == uid && w.ProductId == productId);
        bool liked;
        if (existing != null) { _db.Wishlists.Remove(existing); liked = false; }
        else
        {
            if (!await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive)) return NotFound();
            _db.Wishlists.Add(new Wishlist { UserId = uid, ProductId = productId });
            liked = true;
        }
        await _db.SaveChangesAsync();
        return Json(new { liked });
    }
}