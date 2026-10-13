// File: BannersController.cs
namespace FashionStore.Areas.Admin.Controllers;

public class BannersController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IFileService _files;
    public BannersController(ApplicationDbContext db, IFileService files) { _db = db; _files = files; }

    public async Task<IActionResult> Index() => View(await _db.Banners.OrderBy(b => b.SortOrder).ToListAsync());

    [HttpGet] public IActionResult Create() => View("Form", new Banner { SortOrder = 1 });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,ImageUrl,LinkUrl,SortOrder,IsActive")] Banner model, IFormFile? imageFile)
    {
        try { if (imageFile is { Length: > 0 }) model.ImageUrl = await _files.SaveImageAsync(imageFile, "banners"); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        if (string.IsNullOrWhiteSpace(model.ImageUrl)) ModelState.AddModelError("", "Hãy chọn ảnh hoặc nhập đường dẫn ảnh");
        if (!ModelState.IsValid) return View("Form", model);
        _db.Banners.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm banner";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        return b == null ? NotFound() : View("Form", b);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Title,ImageUrl,LinkUrl,SortOrder,IsActive")] Banner model, IFormFile? imageFile)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b == null) return NotFound();
        b.Title = model.Title; b.LinkUrl = model.LinkUrl; b.SortOrder = model.SortOrder; b.IsActive = model.IsActive;
        try
        {
            if (imageFile is { Length: > 0 }) { _files.Delete(b.ImageUrl); b.ImageUrl = await _files.SaveImageAsync(imageFile, "banners"); }
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl)) b.ImageUrl = model.ImageUrl.Trim();
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        if (string.IsNullOrWhiteSpace(b.ImageUrl)) ModelState.AddModelError("", "Banner cần có ảnh");
        if (!ModelState.IsValid) return View("Form", b);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật banner";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b != null) { _files.Delete(b.ImageUrl); _db.Banners.Remove(b); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa banner"; }
        return RedirectToAction(nameof(Index));
    }
}