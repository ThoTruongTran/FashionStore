namespace FashionStore.Models.Entities;
[Table("Users")]
public class AppUser
{
    public int Id { get; set; }
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required] public string PasswordHash { get; set; } = string.Empty;
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [StringLength(15)] public string? PhoneNumber { get; set; }
    [StringLength(10)] public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}