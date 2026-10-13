// File: AttributesController.cs   (quản lý Màu sắc và Kích cỡ)
using System.Text.RegularExpressions;
namespace FashionStore.Areas.Admin.Controllers;

public class AttributesController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public AttributesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.Colors = await _db.Colors.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Sizes = await _db.Sizes.OrderBy(s => s.SortOrder).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddColor(string name, string hexCode)
    {
        if (string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(hexCode ?? "", "^#[0-9A-Fa-f]{6}$"))
            TempData["Error"] = "Nhập tên màu và mã màu dạng #RRGGBB";
        else if (await _db.Colors.AnyAsync(c => c.Name == name.Trim())) TempData["Error"] = "Màu này đã tồn tại";
        else { _db.Colors.Add(new Color { Name = name.Trim(), HexCode = hexCode! }); await _db.SaveChangesAsync(); TempData["Success"] = "Đã thêm màu"; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteColor(int id)
    {
        if (await _db.ProductVariants.AnyAsync(v => v.ColorId == id)) TempData["Error"] = "Màu đang được dùng trong sản phẩm, không thể xóa";
        else { var c = await _db.Colors.FindAsync(id); if (c != null) { _db.Colors.Remove(c); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa màu"; } }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSize(string name, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name)) TempData["Error"] = "Nhập tên size";
        else if (await _db.Sizes.AnyAsync(s => s.Name == name.Trim())) TempData["Error"] = "Size này đã tồn tại";
        else { _db.Sizes.Add(new Size { Name = name.Trim(), SortOrder = sortOrder }); await _db.SaveChangesAsync(); TempData["Success"] = "Đã thêm size"; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSize(int id)
    {
        if (await _db.ProductVariants.AnyAsync(v => v.SizeId == id)) TempData["Error"] = "Size đang được dùng trong sản phẩm, không thể xóa";
        else { var s = await _db.Sizes.FindAsync(id); if (s != null) { _db.Sizes.Remove(s); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa size"; } }
        return RedirectToAction(nameof(Index));
    }
}