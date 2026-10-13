namespace FashionStore.Models.Entities;
public class ShippingMethod
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Fee { get; set; }
    public int EstimatedDays { get; set; }
    public bool IsActive { get; set; } = true;
}