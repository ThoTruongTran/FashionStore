namespace FashionStore.Models.Entities;
public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    [NotMapped] public decimal Total => Items.Sum(i => i.Quantity * (i.ProductVariant?.FinalPrice ?? 0));
}