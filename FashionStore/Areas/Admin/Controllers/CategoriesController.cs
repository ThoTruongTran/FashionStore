// File: CategoriesController.cs
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionStore.Areas.Admin.Controllers;

public class CategoriesController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var all = await _db.Categories.Include(c => c.ParentCategory).ToListAsync();
        var sorted = all.Where(c => c.ParentCategoryId == null).OrderBy(c => c.Name)
            .SelectMany(p => new[] { p }.Concat(all.Where(c => c.ParentCategoryId == p.Id).OrderBy(c => c.Name))).ToList();
        ViewBag.ProductCounts = await _db.Products.GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return View(sorted);
    }

    [HttpGet] public async Task<IActionResult> Create() { await LoadParents(0); return View("Form", new Category()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,ParentCategoryId,IsActive")] Category model)
    {
        if (!await ValidateParent(model, 0)) { await LoadParents(0); return View("Form", model); }
        model.Slug = model.Name.ToSlug();
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm danh mục";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return NotFound();
        await LoadParents(id);
        return View("Form", c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,ParentCategoryId,IsActive")] Category model)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return NotFound();
        c.Name = model.Name; c.ParentCategoryId = model.ParentCategoryId; c.IsActive = model.IsActive;
        if (!await ValidateParent(c, id)) { await LoadParents(id); return View("Form", c); }
        c.Slug = c.Name.ToSlug();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật danh mục";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return NotFound();
        if (await _db.Products.AnyAsync(p => p.CategoryId == id) || await _db.Categories.AnyAsync(x => x.ParentCategoryId == id))
            TempData["Error"] = "Không thể xóa: danh mục còn sản phẩm hoặc danh mục con";
        else { _db.Categories.Remove(c); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa danh mục"; }
        return RedirectToAction(nameof(Index));
    }

    // Chỉ cho phép 2 cấp: cha -> con
    private async Task<bool> ValidateParent(Category c, int id)
    {
        if (!ModelState.IsValid) return false;
        if (c.ParentCategoryId == null) return true;
        if (c.ParentCategoryId == id && id != 0) { ModelState.AddModelError("", "Danh mục không thể là cha của chính nó"); return false; }
        if (id != 0 && await _db.Categories.AnyAsync(x => x.ParentCategoryId == id))
        { ModelState.AddModelError("", "Danh mục đang có danh mục con nên không thể gán danh mục cha"); return false; }
        var parent = await _db.Categories.FindAsync(c.ParentCategoryId);
        if (parent?.ParentCategoryId != null) { ModelState.AddModelError("", "Chỉ hỗ trợ 2 cấp danh mục"); return false; }
        return true;
    }

    private async Task LoadParents(int excludeId) =>
        ViewBag.Parents = new SelectList(await _db.Categories.Where(c => c.ParentCategoryId == null && c.Id != excludeId)
            .OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
}   