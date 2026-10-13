namespace FashionStore.Services.Interfaces;
public interface ICouponService
{
    Task<(bool Ok, string? Error, Coupon? Coupon, decimal Discount)> ValidateAsync(string code, decimal subTotal);
}