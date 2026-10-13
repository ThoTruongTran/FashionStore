// File: BrandsController.cs
namespace FashionStore.Areas.Admin.Controllers;

public class BrandsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IFileService _files;
    public BrandsController(ApplicationDbContext db, IFileService files) { _db = db; _files = files; }

    public async Task<IActionResult> Index()
    {
        ViewBag.Counts = await _db.Products.GroupBy(p => p.BrandId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return View(await _db.Brands.OrderBy(b => b.Name).ToListAsync());
    }

    [HttpGet] public IActionResult Create() => View("Form", new Brand());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,LogoUrl")] Brand model, IFormFile? logoFile)
    {
        if (!ModelState.IsValid) return View("Form", model);
        try { if (logoFile is { Length: > 0 }) model.LogoUrl = await _files.SaveImageAsync(logoFile, "brands"); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View("Form", model); }
        model.Slug = model.Name.ToSlug();
        _db.Brands.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm thương hiệu";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var b = await _db.Brands.FindAsync(id);
        return b == null ? NotFound() : View("Form", b);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Description,LogoUrl")] Brand model, IFormFile? logoFile)
    {
        var b = await _db.Brands.FindAsync(id);
        if (b == null) return NotFound();
        b.Name = model.Name; b.Description = model.Description;
        if (!ModelState.IsValid) return View("Form", b);
        try
        {
            if (logoFile is { Length: > 0 }) { _files.Delete(b.LogoUrl); b.LogoUrl = await _files.SaveImageAsync(logoFile, "brands"); }
            else b.LogoUrl = string.IsNullOrWhiteSpace(model.LogoUrl) ? null : model.LogoUrl.Trim();
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View("Form", b); }
        b.Slug = b.Name.ToSlug();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thương hiệu";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Brands.FindAsync(id);
        if (b == null) return NotFound();
        if (await _db.Products.AnyAsync(p => p.BrandId == id)) TempData["Error"] = "Không thể xóa: thương hiệu còn sản phẩm";
        else { _files.Delete(b.LogoUrl); _db.Brands.Remove(b); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa thương hiệu"; }
        return RedirectToAction(nameof(Index));
    }
}