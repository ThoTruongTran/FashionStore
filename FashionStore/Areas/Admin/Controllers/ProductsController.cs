// File: ProductsController.cs
using Microsoft.AspNetCore.Mvc.Rendering;
namespace FashionStore.Areas.Admin.Controllers;

public class ProductsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IFileService _files;
    public ProductsController(ApplicationDbContext db, IFileService files) { _db = db; _files = files; }

    public async Task<IActionResult> Index(string? keyword, int? categoryId, string? status, int page = 1)
    {
        const int pageSize = 10;
        var q = _db.Products.Include(p => p.Images).Include(p => p.Category).Include(p => p.Brand)
            .Include(p => p.Variants).AsSplitQuery().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) { var k = keyword.Trim(); q = q.Where(p => p.Name.Contains(k)); }
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (status == "active") q = q.Where(p => p.IsActive);
        else if (status == "hidden") q = q.Where(p => !p.IsActive);

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        ViewBag.Paging = new PagingInfo { Page = page, TotalPages = totalPages };
        ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Keyword = keyword; ViewBag.Status = status; ViewBag.Total = total;
        return View(await q.OrderByDescending(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var p = new Product();
        await LoadListsAsync(p);
        return View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,BasePrice,DiscountPercent,Material,Gender,IsActive,CategoryId,BrandId")] Product model,
        List<IFormFile> newImages)
    {
        if (model.CategoryId == 0) ModelState.AddModelError(nameof(model.CategoryId), "Chọn danh mục");
        if (model.BrandId == 0) ModelState.AddModelError(nameof(model.BrandId), "Chọn thương hiệu");
        if (ModelState.IsValid)
        {
            try { await SaveImagesAsync(model, newImages); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        if (!ModelState.IsValid) { await LoadListsAsync(model); return View("Form", model); }

        model.Slug = await UniqueSlugAsync(model.Name, 0);
        model.CreatedAt = DateTime.Now;
        _db.Products.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo sản phẩm. Hãy thêm biến thể (màu/size/tồn kho) ở bên dưới.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await LoadProductAsync(id);
        if (p == null) return NotFound();
        await LoadListsAsync(p);
        return View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Name,Description,BasePrice,DiscountPercent,Material,Gender,IsActive,CategoryId,BrandId")] Product model,
        List<IFormFile> newImages)
    {
        var p = await LoadProductAsync(id);
        if (p == null) return NotFound();

        var nameChanged = p.Name != model.Name;
        p.Name = model.Name; p.Description = model.Description; p.BasePrice = model.BasePrice;
        p.DiscountPercent = model.DiscountPercent; p.Material = model.Material; p.Gender = model.Gender;
        p.IsActive = model.IsActive; p.CategoryId = model.CategoryId; p.BrandId = model.BrandId;

        if (ModelState.IsValid)
        {
            try { await SaveImagesAsync(p, newImages); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        if (!ModelState.IsValid) { await LoadListsAsync(p); return View("Form", p); }

        if (nameChanged) p.Slug = await UniqueSlugAsync(p.Name, p.Id);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu sản phẩm";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = !p.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = p.IsActive ? "Đã hiển thị sản phẩm" : "Đã ẩn sản phẩm";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        if (await _db.OrderDetails.AnyAsync(d => d.ProductVariant!.ProductId == id))
        {   // đã từng bán -> chỉ ẩn để giữ lịch sử đơn hàng
            p.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Error"] = "Sản phẩm đã có trong đơn hàng nên không thể xóa, hệ thống đã chuyển sang trạng thái ẨN";
        }
        else
        {
            foreach (var im in p.Images) _files.Delete(im.ImageUrl);
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa sản phẩm";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- Ảnh ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage(int id)
    {
        var im = await _db.ProductImages.FindAsync(id);
        if (im == null) return NotFound();
        var all = await _db.ProductImages.Where(i => i.ProductId == im.ProductId).ToListAsync();
        foreach (var i in all) i.IsMain = i.Id == id;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = im.ProductId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var im = await _db.ProductImages.FindAsync(id);
        if (im == null) return NotFound();
        var productId = im.ProductId;
        _files.Delete(im.ImageUrl);
        _db.ProductImages.Remove(im);
        await _db.SaveChangesAsync();

        if (!await _db.ProductImages.AnyAsync(i => i.ProductId == productId && i.IsMain))
        {   // xóa ảnh chính thì tự chọn ảnh khác làm ảnh chính
            var next = await _db.ProductImages.Where(i => i.ProductId == productId).OrderBy(i => i.SortOrder).FirstOrDefaultAsync();
            if (next != null) { next.IsMain = true; await _db.SaveChangesAsync(); }
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    // ---------- Biến thể ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(int productId, int colorId, int sizeId, int stockQuantity, decimal extraPrice)
    {
        if (colorId == 0 || sizeId == 0 || stockQuantity < 0) TempData["Error"] = "Chọn màu, size và nhập tồn kho hợp lệ";
        else if (await _db.ProductVariants.AnyAsync(v => v.ProductId == productId && v.ColorId == colorId && v.SizeId == sizeId))
            TempData["Error"] = "Biến thể màu/size này đã tồn tại";
        else
        {
            _db.ProductVariants.Add(new ProductVariant
            {
                ProductId = productId,
                ColorId = colorId,
                SizeId = sizeId,
                StockQuantity = stockQuantity,
                ExtraPrice = extraPrice,
                Sku = $"P{productId}-C{colorId}-S{sizeId}"
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm biến thể";
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVariant(int id, int stockQuantity, decimal extraPrice)
    {
        var v = await _db.ProductVariants.FindAsync(id);
        if (v == null) return NotFound();
        if (stockQuantity < 0) TempData["Error"] = "Tồn kho không được âm";
        else { v.StockQuantity = stockQuantity; v.ExtraPrice = extraPrice; await _db.SaveChangesAsync(); TempData["Success"] = "Đã cập nhật biến thể"; }
        return RedirectToAction(nameof(Edit), new { id = v.ProductId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(int id)
    {
        var v = await _db.ProductVariants.FindAsync(id);
        if (v == null) return NotFound();
        if (await _db.OrderDetails.AnyAsync(d => d.ProductVariantId == id))
            TempData["Error"] = "Biến thể đã có trong đơn hàng, không thể xóa. Hãy đặt tồn kho = 0.";
        else { _db.ProductVariants.Remove(v); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa biến thể"; }
        return RedirectToAction(nameof(Edit), new { id = v.ProductId });
    }

    // ---------- Hàm phụ ----------
    private Task<Product?> LoadProductAsync(int id) =>
        _db.Products.Include(x => x.Images)
            .Include(x => x.Variants).ThenInclude(v => v.Color)
            .Include(x => x.Variants).ThenInclude(v => v.Size)
            .AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id);

    private async Task LoadListsAsync(Product p)
    {
        var cats = await _db.Categories.Include(c => c.ParentCategory).Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(cats.Select(c => new
        {
            c.Id,
            Text = c.ParentCategory != null ? $"{c.ParentCategory.Name} › {c.Name}" : c.Name
        }).OrderBy(x => x.Text), "Id", "Text", p.CategoryId);
        ViewBag.Brands = new SelectList(await _db.Brands.OrderBy(b => b.Name).ToListAsync(), "Id", "Name", p.BrandId);
        ViewBag.Colors = await _db.Colors.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Sizes = await _db.Sizes.OrderBy(s => s.SortOrder).ToListAsync();
    }

    private async Task SaveImagesAsync(Product p, List<IFormFile>? files)
    {
        if (files == null) return;
        var order = p.Images.Count == 0 ? 0 : p.Images.Max(i => i.SortOrder) + 1;
        foreach (var f in files.Where(f => f.Length > 0))
        {
            var url = await _files.SaveImageAsync(f, "products");
            p.Images.Add(new ProductImage { ImageUrl = url, SortOrder = order++, IsMain = !p.Images.Any(i => i.IsMain) });
        }
    }

    private async Task<string> UniqueSlugAsync(string name, int excludeId)
    {
        var baseSlug = name.ToSlug(); var slug = baseSlug; var i = 2;
        while (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != excludeId)) slug = $"{baseSlug}-{i++}";
        return slug;
    }
}