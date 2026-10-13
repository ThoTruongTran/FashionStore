// File: Models/ViewModels/AdminViewModels.cs
namespace FashionStore.Models.ViewModels;

public class NameValue { public string Name { get; set; } = ""; public long Value { get; set; } }
public class DayRevenue { public DateTime Date { get; set; } public decimal Revenue { get; set; } public int Orders { get; set; } }

public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }

    public List<NameValue> MonthlyRevenue { get; set; } = new();
    public List<NameValue> OrdersByStatus { get; set; } = new();
    public List<NameValue> TopProducts { get; set; } = new();
    public List<DayRevenue> Last7Days { get; set; } = new();
    public List<ProductVariant> LowStock { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
}

public class AdminUserCreateViewModel
{
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)] public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")] public string Email { get; set; } = "";
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")] public string? PhoneNumber { get; set; }
    [Required(ErrorMessage = "Nhập mật khẩu"), StringLength(50, MinimumLength = 6, ErrorMessage = "Tối thiểu 6 ký tự"), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Chọn vai trò")] public int RoleId { get; set; }
}