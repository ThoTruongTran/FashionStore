namespace FashionStore.Helpers;
public static class StatusHelper
{
    public static string ToLabel(this OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };
    public static string ToBadge(this OrderStatus s) => s switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Confirmed => "bg-info text-dark",
        OrderStatus.Shipping => "bg-primary",
        OrderStatus.Completed => "bg-success",
        OrderStatus.Cancelled => "bg-secondary",
        _ => "bg-light text-dark"
    };
    public static string ToLabel(this PaymentStatus s) => s switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.Failed => "Thất bại",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => s.ToString()
    };

    public static OrderStatus[] NextStatuses(this OrderStatus s) => s switch
    {
        OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        OrderStatus.Confirmed => new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
        OrderStatus.Shipping => new[] { OrderStatus.Completed },
        _ => Array.Empty<OrderStatus>()
    };

    public static string ToBadge(this PaymentStatus s) => s switch
    {
        PaymentStatus.Paid => "bg-success",
        PaymentStatus.Failed => "bg-danger",
        PaymentStatus.Refunded => "bg-info text-dark",
        _ => "bg-warning text-dark"
    };
}