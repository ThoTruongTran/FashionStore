namespace FashionStore.Models.Entities;
public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int ColorId { get; set; }
    public Color? Color { get; set; }
    public int SizeId { get; set; }
    public Size? Size { get; set; }
    [StringLength(50)] public string Sku { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ExtraPrice { get; set; }

    [NotMapped] public decimal FinalPrice => (Product?.SalePrice ?? 0) + ExtraPrice;
}