namespace FashionStore.Services.Interfaces;
public interface IOrderService
{
    Task<(bool Ok, string? Error, Order? Order)> PlaceOrderAsync(int userId, CheckoutViewModel vm);
    Task<(bool Ok, string? Error)> CancelAsync(int userId, int orderId);

    Task<(bool Ok, string? Error)> UpdateStatusAsync(int orderId, OrderStatus newStatus, int staffId, string? note);
}