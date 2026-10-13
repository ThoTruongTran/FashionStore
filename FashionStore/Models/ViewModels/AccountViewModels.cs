// File: AccountViewModels.cs
namespace FashionStore.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự"), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    public string FullName { get; set; } = "";
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    [DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại"), DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu mới"), StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự"), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required] public string Token { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu mới"), StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự"), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}