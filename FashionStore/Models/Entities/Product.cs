namespace FashionStore.Models.Entities;
public class Product
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [StringLength(220)] public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BasePrice { get; set; }
    [Range(0, 100)] public int DiscountPercent { get; set; }
    [StringLength(100)] public string? Material { get; set; }
    [StringLength(20)] public string? Gender { get; set; }   // Nam / Nữ / Unisex
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int BrandId { get; set; }
    public Brand? Brand { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    [NotMapped] public decimal SalePrice => Math.Round(BasePrice * (100 - DiscountPercent) / 100, 0);
}