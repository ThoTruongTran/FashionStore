// File: ReviewsController.cs
namespace FashionStore.Areas.Admin.Controllers;

public class ReviewsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public ReviewsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        const int pageSize = 10;
        var q = _db.Reviews.Include(r => r.Product).Include(r => r.User).AsQueryable();
        if (status == "approved") q = q.Where(r => r.IsApproved);
        else if (status == "hidden") q = q.Where(r => !r.IsApproved);

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        ViewBag.Paging = new PagingInfo { Page = page, TotalPages = totalPages };
        ViewBag.Status = status;
        return View(await q.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r == null) return NotFound();
        r.IsApproved = !r.IsApproved;
        await _db.SaveChangesAsync();
        TempData["Success"] = r.IsApproved ? "Đã hiển thị đánh giá" : "Đã ẩn đánh giá";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r != null) { _db.Reviews.Remove(r); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa đánh giá"; }
        return RedirectToAction(nameof(Index));
    }
}