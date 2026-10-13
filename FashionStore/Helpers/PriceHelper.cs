using System.Globalization;
namespace FashionStore.Helpers;
public static class PriceHelper
{
    private static readonly CultureInfo Vn = CultureInfo.GetCultureInfo("vi-VN");
    public static string ToVnd(this decimal value) => value.ToString("N0", Vn) + "₫";
}