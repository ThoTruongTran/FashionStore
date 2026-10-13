// File: Controllers/ProductController.cs
namespace FashionStore.Controllers;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProductController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index([FromQuery] ProductFilter filter)
    {
        const int pageSize = 12;
        var query = _db.Products.Include(p => p.Images).Include(p => p.Brand).Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(k) || p.Brand!.Name.Contains(k));
        }
        if (filter.CategoryId.HasValue)
        {
            var cid = filter.CategoryId.Value;
            query = query.Where(p => p.CategoryId == cid || p.Category!.ParentCategoryId == cid);
        }
        if (filter.BrandId.HasValue)
        {
            var bid = filter.BrandId.Value;
            query = query.Where(p => p.BrandId == bid);
        }
        if (!string.IsNullOrEmpty(filter.Gender))
        {
            var g = filter.Gender;
            query = query.Where(p => p.Gender == g || p.Gender == "Unisex");
        }
        if (filter.MinPrice.HasValue)
        {
            var min = filter.MinPrice.Value;
            query = query.Where(p => p.BasePrice * (100 - p.DiscountPercent) / 100 >= min);
        }
        if (filter.MaxPrice.HasValue)
        {
            var max = filter.MaxPrice.Value;
            query = query.Where(p => p.BasePrice * (100 - p.DiscountPercent) / 100 <= max);
        }

        query = filter.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice * (100 - p.DiscountPercent) / 100),
            "price_desc" => query.OrderByDescending(p => p.BasePrice * (100 - p.DiscountPercent) / 100),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        filter.Page = Math.Clamp(filter.Page, 1, totalPages);

        var vm = new ProductListViewModel
        {
            Filter = filter,
            TotalCount = total,
            Paging = new PagingInfo { Page = filter.Page, TotalPages = totalPages },
            Products = await query.Skip((filter.Page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
            Brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync()
        };
        return View(vm);
    }

    [Route("san-pham/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var product = await _db.Products
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Reviews.Where(r => r.IsApproved)).ThenInclude(r => r.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
        if (product == null) return NotFound();

        var vm = new ProductDetailViewModel
        {
            Product = product,
            Colors = product.Variants.Select(v => v.Color!).DistinctBy(c => c.Id).ToList(),
            Sizes = product.Variants.Select(v => v.Size!).DistinctBy(s => s.Id).OrderBy(s => s.SortOrder).ToList(),
            AvgRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0,
            Related = await _db.Products.Include(p => p.Images).Include(p => p.Brand)
                .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4).ToListAsync()
        };
        if (User.Identity?.IsAuthenticated == true)
        {
            var uid = User.GetUserId();
            vm.CanReview = await _db.OrderDetails.AnyAsync(d =>
                d.ProductVariant!.ProductId == product.Id && d.Order!.UserId == uid && d.Order.Status == OrderStatus.Completed);
            vm.MyReview = await _db.Reviews.FirstOrDefaultAsync(r => r.ProductId == product.Id && r.UserId == uid);
        }
        return View(vm);
    }
}