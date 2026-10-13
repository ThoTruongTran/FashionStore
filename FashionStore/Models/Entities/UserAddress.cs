namespace FashionStore.Models.Entities;
public class UserAddress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    [Required, StringLength(100)] public string ReceiverName { get; set; } = string.Empty;
    [Required, StringLength(15)] public string Phone { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Province { get; set; } = string.Empty;
    [Required, StringLength(100)] public string District { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Ward { get; set; } = string.Empty;
    [Required, StringLength(200)] public string StreetDetail { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}