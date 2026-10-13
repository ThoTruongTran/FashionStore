namespace FashionStore.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICouponService _coupons;
    public OrderService(ApplicationDbContext db, ICouponService coupons) { _db = db; _coupons = coupons; }

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

        var subTotal = cart.Items.Sum(i => i.Quantity * i.ProductVariant!.FinalPrice);

        await using var tx = await _db.Database.BeginTransactionAsync();   // return sớm = tự rollback

        // ----- Mã giảm giá -----
        Coupon? coupon = null; decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(vm.CouponCode))
        {
            var r = await _coupons.ValidateAsync(vm.CouponCode, subTotal);
            if (!r.Ok) return (false, r.Error, null);
            coupon = r.Coupon; discount = r.Discount;

            // Trừ lượt dùng bằng 1 câu UPDATE có điều kiện để 2 khách không dùng chung lượt cuối
            var rows = await _db.Coupons
                .Where(c => c.Id == coupon!.Id && (c.MaxUsage == 0 || c.UsedCount < c.MaxUsage))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1));
            if (rows == 0) return (false, "Mã giảm giá đã hết lượt sử dụng", null);
        }

        var order = new Order
        {
            OrderCode = $"FS{DateTime.Now:yyMMddHHmmss}{Random.Shared.Next(10, 99)}",
            UserId = userId,
            ReceiverName = vm.ReceiverName.Trim(),
            ReceiverPhone = vm.ReceiverPhone.Trim(),
            ShippingAddress = $"{vm.StreetDetail}, {vm.Ward}, {vm.District}, {vm.Province}",
            SubTotal = subTotal,
            ShippingFee = shipping.Fee,
            DiscountAmount = discount,
            TotalAmount = Math.Max(0, subTotal + shipping.Fee - discount),
            CouponId = coupon?.Id,
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
            v.StockQuantity -= item.Quantity;
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
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) return (false, "Không tìm thấy đơn hàng");
        if (order.Status != OrderStatus.Pending) return (false, "Chỉ hủy được đơn đang chờ xác nhận");

        foreach (var d in order.Details) d.ProductVariant!.StockQuantity += d.Quantity;   // hoàn kho
        MarkPaymentsOnCancel(order);
        order.Status = OrderStatus.Cancelled;
        order.StatusHistories.Add(new OrderStatusHistory { Status = OrderStatus.Cancelled, ChangedByUserId = userId, Note = "Khách hủy đơn" });
        await _db.SaveChangesAsync();
        await ReleaseCouponAsync(order);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateStatusAsync(int orderId, OrderStatus newStatus, int staffId, string? note)
    {
        var order = await _db.Orders
            .Include(o => o.Details).ThenInclude(d => d.ProductVariant)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return (false, "Không tìm thấy đơn hàng");

        if (!order.Status.NextStatuses().Contains(newStatus))
            return (false, $"Không thể chuyển từ \"{order.Status.ToLabel()}\" sang \"{newStatus.ToLabel()}\"");

        if (newStatus == OrderStatus.Cancelled)
        {
            foreach (var d in order.Details) d.ProductVariant!.StockQuantity += d.Quantity;
            MarkPaymentsOnCancel(order);
        }
        if (newStatus == OrderStatus.Completed)
            foreach (var p in order.Payments.Where(p => p.Status == PaymentStatus.Unpaid))
            { p.Status = PaymentStatus.Paid; p.PaidAt = DateTime.Now; }          // COD: giao xong là đã thu tiền

        order.Status = newStatus;
        order.StatusHistories.Add(new OrderStatusHistory { Status = newStatus, ChangedByUserId = staffId, Note = note });
        await _db.SaveChangesAsync();
        if (newStatus == OrderStatus.Cancelled) await ReleaseCouponAsync(order);
        return (true, null);
    }

    private static void MarkPaymentsOnCancel(Order order)
    {
        foreach (var p in order.Payments)
            p.Status = p.Status == PaymentStatus.Paid ? PaymentStatus.Refunded : PaymentStatus.Failed;
    }

    // Hủy đơn thì trả lại 1 lượt dùng mã
    private Task ReleaseCouponAsync(Order order) =>
        order.CouponId == null ? Task.CompletedTask
        : _db.Coupons.Where(c => c.Id == order.CouponId && c.UsedCount > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount - 1));
}