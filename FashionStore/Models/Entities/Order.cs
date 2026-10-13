namespace FashionStore.Models.Entities;
public class Order
{
    public int Id { get; set; }
    [StringLength(30)] public string OrderCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(100)] public string ReceiverName { get; set; } = string.Empty;
    [StringLength(15)] public string ReceiverPhone { get; set; } = string.Empty;
    [StringLength(500)] public string ShippingAddress { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")] public decimal SubTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ShippingFee { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    [StringLength(500)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public int ShippingMethodId { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }

    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}