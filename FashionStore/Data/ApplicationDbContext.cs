namespace FashionStore.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Banner> Banners => Set<Banner>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ----- Người dùng & phân quyền -----
        b.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<AppUser>().HasOne(u => u.Role).WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);

        // ----- Danh mục, sản phẩm -----
        b.Entity<Category>().HasOne(c => c.ParentCategory).WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>().HasOne(p => p.Brand).WithMany(x => x.Products)
            .HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ProductVariant>().HasIndex(v => new { v.ProductId, v.ColorId, v.SizeId }).IsUnique();
        b.Entity<ProductVariant>().HasOne(v => v.Color).WithMany()
            .HasForeignKey(v => v.ColorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductVariant>().HasOne(v => v.Size).WithMany()
            .HasForeignKey(v => v.SizeId).OnDelete(DeleteBehavior.Restrict);

        // ----- Giỏ hàng, wishlist, đánh giá -----
        b.Entity<Cart>().HasIndex(c => c.UserId).IsUnique();
        b.Entity<CartItem>().HasIndex(i => new { i.CartId, i.ProductVariantId }).IsUnique();
        b.Entity<Wishlist>().HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
        b.Entity<Review>().HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();
        b.Entity<Review>().HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        // ----- Đơn hàng -----
        b.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        b.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
        b.Entity<Order>().HasOne(o => o.User).WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>().HasOne(o => o.ShippingMethod).WithMany()
            .HasForeignKey(o => o.ShippingMethodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderDetail>().HasOne(d => d.ProductVariant).WithMany()
            .HasForeignKey(d => d.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderStatusHistory>().HasOne(h => h.ChangedByUser).WithMany()
            .HasForeignKey(h => h.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Payment>().HasOne(p => p.PaymentMethod).WithMany()
            .HasForeignKey(p => p.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}