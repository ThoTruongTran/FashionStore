using System.Text.RegularExpressions;
namespace FashionStore.Areas.Admin.Controllers;

public class CouponsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public CouponsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Coupons.OrderByDescending(c => c.Id).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View("Form", new Coupon
    {
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddDays(30),
        MaxUsage = 100,
        DiscountValue = 10
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,DiscountType,DiscountValue,MinOrderAmount,MaxUsage,StartDate,EndDate,IsActive")] Coupon model)
    {
        if (!await ValidateAsync(model, 0)) return View("Form", model);
        _db.Coupons.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo mã giảm giá";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        return c == null ? NotFound() : View("Form", c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Code,DiscountType,DiscountValue,MinOrderAmount,MaxUsage,StartDate,EndDate,IsActive")] Coupon model)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null) return NotFound();
        c.Code = model.Code; c.DiscountType = model.DiscountType; c.DiscountValue = model.DiscountValue;
        c.MinOrderAmount = model.MinOrderAmount; c.MaxUsage = model.MaxUsage;
        c.StartDate = model.StartDate; c.EndDate = model.EndDate; c.IsActive = model.IsActive;
        if (!await ValidateAsync(c, id)) return View("Form", c);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật mã giảm giá";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null) return NotFound();
        if (await _db.Orders.AnyAsync(o => o.CouponId == id))
        {
            c.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Error"] = "Mã đã được dùng trong đơn hàng nên không thể xóa, hệ thống đã chuyển sang TẮT";
        }
        else { _db.Coupons.Remove(c); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa mã giảm giá"; }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidateAsync(Coupon c, int id)
    {
        c.Code = (c.Code ?? "").Trim().ToUpper();
        if (!Regex.IsMatch(c.Code, "^[A-Z0-9_-]{3,30}$")) ModelState.AddModelError("", "Mã gồm 3-30 ký tự chữ, số, gạch ngang hoặc gạch dưới");
        if (c.DiscountValue <= 0) ModelState.AddModelError("", "Giá trị giảm phải lớn hơn 0");
        if (c.DiscountType == DiscountType.Percent && c.DiscountValue > 100) ModelState.AddModelError("", "Giảm theo % không được quá 100");
        if (c.MinOrderAmount < 0 || c.MaxUsage < 0) ModelState.AddModelError("", "Đơn tối thiểu và số lượt không được âm");
        if (c.EndDate.Date < c.StartDate.Date) ModelState.AddModelError("", "Ngày kết thúc phải sau ngày bắt đầu");
        if (await _db.Coupons.AnyAsync(x => x.Code == c.Code && x.Id != id)) ModelState.AddModelError("", "Mã này đã tồn tại");
        return ModelState.IsValid;
    }
}