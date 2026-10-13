namespace FashionStore.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReviewController(ApplicationDbContext db) => _db = db;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int productId, int rating, string? comment)
    {
        var uid = User.GetUserId();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return NotFound();

        var purchased = await _db.OrderDetails.AnyAsync(d =>
            d.ProductVariant!.ProductId == productId && d.Order!.UserId == uid && d.Order.Status == OrderStatus.Completed);
        if (!purchased) TempData["Error"] = "Chỉ khách đã mua và nhận hàng thành công mới được đánh giá sản phẩm này";
        else if (rating < 1 || rating > 5) TempData["Error"] = "Số sao phải từ 1 đến 5";
        else
        {
            comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            if (comment != null && comment.Length > 1000) comment = comment[..1000];

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == uid);
            if (review == null) _db.Reviews.Add(new Review { ProductId = productId, UserId = uid, Rating = rating, Comment = comment });
            else { review.Rating = rating; review.Comment = comment; review.CreatedAt = DateTime.Now; }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm";
        }
        return RedirectToAction("Details", "Product", new { slug = product.Slug });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int productId)
    {
        var uid = User.GetUserId();
        var review = await _db.Reviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == uid);
        if (review == null) return NotFound();
        var slug = review.Product!.Slug;
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa đánh giá của bạn";
        return RedirectToAction("Details", "Product", new { slug });
    }
}