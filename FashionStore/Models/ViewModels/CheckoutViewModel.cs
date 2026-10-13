// File: CheckoutViewModel.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace FashionStore.Models.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Nhập họ tên người nhận"), StringLength(100)] public string ReceiverName { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại"), RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string ReceiverPhone { get; set; } = "";
    [Required(ErrorMessage = "Nhập tỉnh/thành phố"), StringLength(100)] public string Province { get; set; } = "";
    [Required(ErrorMessage = "Nhập quận/huyện"), StringLength(100)] public string District { get; set; } = "";
    [Required(ErrorMessage = "Nhập phường/xã"), StringLength(100)] public string Ward { get; set; } = "";
    [Required(ErrorMessage = "Nhập số nhà, tên đường"), StringLength(200)] public string StreetDetail { get; set; } = "";
    [StringLength(500)] public string? Note { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Chọn phương thức giao hàng")] public int ShippingMethodId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Chọn phương thức thanh toán")] public int PaymentMethodId { get; set; }
    public bool SaveAddress { get; set; }

    [ValidateNever] public Cart Cart { get; set; } = new();
    [ValidateNever] public List<ShippingMethod> ShippingMethods { get; set; } = new();
    [ValidateNever] public List<PaymentMethod> PaymentMethods { get; set; } = new();
}