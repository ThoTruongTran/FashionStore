namespace FashionStore.Models.Enums;

public enum OrderStatus { Pending = 0, Confirmed = 1, Shipping = 2, Completed = 3, Cancelled = 4 }
public enum PaymentStatus { Unpaid = 0, Paid = 1, Failed = 2, Refunded = 3 }
public enum DiscountType { Percent = 0, FixedAmount = 1 }