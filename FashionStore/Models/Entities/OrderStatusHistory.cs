namespace FashionStore.Models.Entities;
public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    public int? ChangedByUserId { get; set; }
    public AppUser? ChangedByUser { get; set; }
    [StringLength(300)] public string? Note { get; set; }
}