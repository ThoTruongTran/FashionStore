namespace FashionStore.Models.Entities;
public class Coupon
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountValue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal MinOrderAmount { get; set; }
    public int MaxUsage { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}