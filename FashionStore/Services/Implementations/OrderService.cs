// File: Services/Implementations/OrderService.cs
namespace FashionStore.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    public OrderService(ApplicationDbContext db) => _db = db;

    public async Task<(bool Ok, string? Error, Order? Order)> PlaceOrderAsync(int userId, CheckoutViewModel vm)
    {
        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Product)
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Color)
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Size)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null || !cart.Items.Any()) return (false, "Giỏ hàng trống", null);

        var shipping = await _db.ShippingMethods.FirstOrDefaultAsync(s => s.Id == vm.ShippingMethodId && s.IsActive);
        var payment = await _db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == vm.PaymentMethodId && p.IsActive);
        if (shipping == null || payment == null) return (false, "Phương thức giao hàng hoặc thanh toán không hợp lệ", null);

        foreach (var item in cart.Items)
        {
            var v = item.ProductVariant!;
            if (!v.Product!.IsActive || item.Quantity > v.StockQuantity)
                return (false, $"Sản phẩm \"{v.Product.Name}\" ({v.Color!.Name}/{v.Size!.Name}) không đủ hàng, còn {v.StockQuantity}", null);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var subTotal = cart.Items.Sum(i => i.Quantity * i.ProductVariant!.FinalPrice);
        var order = new Order
        {
            OrderCode = $"FS{DateTime.Now:yyMMddHHmmss}{Random.Shared.Next(10, 99)}",
            UserId = userId,
            ReceiverName = vm.ReceiverName.Trim(),
            ReceiverPhone = vm.ReceiverPhone.Trim(),
            ShippingAddress = $"{vm.StreetDetail}, {vm.Ward}, {vm.District}, {vm.Province}",
            SubTotal = subTotal,
            ShippingFee = shipping.Fee,
            DiscountAmount = 0,
            TotalAmount = subTotal + shipping.Fee,
            Note = vm.Note,
            ShippingMethodId = shipping.Id,
            Status = OrderStatus.Pending
        };

        foreach (var item in cart.Items)
        {
            var v = item.ProductVariant!;
            order.Details.Add(new OrderDetail
            {
                ProductVariantId = v.Id,
                ProductName = $"{v.Product!.Name} ({v.Color!.Name} / {v.Size!.Name})",
                Quantity = item.Quantity,
                UnitPrice = v.FinalPrice
            });
            v.StockQuantity -= item.Quantity;       // trừ tồn kho
        }

        order.StatusHistories.Add(new OrderStatusHistory { Status = OrderStatus.Pending, ChangedByUserId = userId, Note = "Khách đặt hàng" });
        order.Payments.Add(new Payment { PaymentMethodId = payment.Id, Amount = order.TotalAmount, Status = PaymentStatus.Unpaid });
        _db.Orders.Add(order);

        _db.CartItems.RemoveRange(cart.Items);

        if (vm.SaveAddress)
        {
            var hasAny = await _db.UserAddresses.AnyAsync(a => a.UserId == userId);
            _db.UserAddresses.Add(new UserAddress
            {
                UserId = userId,
                ReceiverName = vm.ReceiverName,
                Phone = vm.ReceiverPhone,
                Province = vm.Province,
                District = vm.District,
                Ward = vm.Ward,
                StreetDetail = vm.StreetDetail,
                IsDefault = !hasAny
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null, order);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(int userId, int orderId)
    {
        var order = await _db.Orders.Include(o => o.Details).ThenInclude(d => d.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) return (false, "Không tìm thấy đơn hàng");
        if (order.Status != OrderStatus.Pending) return (false, "Chỉ hủy được đơn đang chờ xác nhận");

        foreach (var d in order.Details) d.ProductVariant!.StockQuantity += d.Quantity;   // hoàn kho
        order.Status = OrderStatus.Cancelled;
        order.StatusHistories.Add(new OrderStatusHistory { Status = OrderStatus.Cancelled, ChangedByUserId = userId, Note = "Khách hủy đơn" });
        await _db.SaveChangesAsync();
        return (true, null);
    }
}