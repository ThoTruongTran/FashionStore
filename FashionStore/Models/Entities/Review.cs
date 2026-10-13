namespace FashionStore.Models.Entities;
public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsApproved { get; set; } = true;
}