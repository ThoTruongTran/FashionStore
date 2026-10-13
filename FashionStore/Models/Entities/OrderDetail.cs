namespace FashionStore.Models.Entities;
public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    [StringLength(300)] public string ProductName { get; set; } = string.Empty;   // bản chụp
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }   // bản chụp
}