namespace FashionStore.Models.Entities;
public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;
    [StringLength(100)] public string? TransactionCode { get; set; }
    public DateTime? PaidAt { get; set; }
}