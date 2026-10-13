// File: CouponService.cs
namespace FashionStore.Services.Implementations;

public class CouponService : ICouponService
{
    private readonly ApplicationDbContext _db;
    public CouponService(ApplicationDbContext db) => _db = db;

    public async Task<(bool Ok, string? Error, Coupon? Coupon, decimal Discount)> ValidateAsync(string code, decimal subTotal)
    {
        code = (code ?? "").Trim().ToUpper();
        if (code.Length == 0) return (false, "Vui lòng nhập mã giảm giá", null, 0);

        var c = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code);
        if (c == null || !c.IsActive) return (false, "Mã giảm giá không tồn tại hoặc đã bị tắt", null, 0);

        var now = DateTime.Now;
        if (now < c.StartDate.Date) return (false, "Mã giảm giá chưa đến ngày áp dụng", null, 0);
        if (now >= c.EndDate.Date.AddDays(1)) return (false, "Mã giảm giá đã hết hạn", null, 0);
        if (c.MaxUsage > 0 && c.UsedCount >= c.MaxUsage) return (false, "Mã giảm giá đã hết lượt sử dụng", null, 0);
        if (subTotal < c.MinOrderAmount) return (false, $"Đơn hàng tối thiểu {c.MinOrderAmount.ToVnd()} để dùng mã này", null, 0);

        var discount = c.DiscountType == DiscountType.Percent
            ? Math.Round(subTotal * c.DiscountValue / 100, 0)
            : c.DiscountValue;
        discount = Math.Min(discount, subTotal);
        return (true, null, c, discount);
    }
}