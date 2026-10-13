using Microsoft.AspNetCore.Identity;

namespace FashionStore.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher<AppUser>>();
        await db.Database.MigrateAsync();

        // ----- Vai trò -----
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(new Role { Name = "Admin" }, new Role { Name = "Staff" }, new Role { Name = "Customer" });
            await db.SaveChangesAsync();
        }

        // ----- Tài khoản mẫu -----
        async Task AddUser(string email, string name, string role, string password)
        {
            if (await db.Users.AnyAsync(u => u.Email == email)) return;
            var r = await db.Roles.FirstAsync(x => x.Name == role);
            var u = new AppUser { Email = email, FullName = name, RoleId = r.Id };
            u.PasswordHash = hasher.HashPassword(u, password);
            db.Users.Add(u);
            await db.SaveChangesAsync();
        }
        await AddUser("admin@fashionstore.vn", "Quản trị viên", "Admin", "Admin@123");
        await AddUser("staff@fashionstore.vn", "Nhân viên bán hàng", "Staff", "Staff@123");
        await AddUser("customer@fashionstore.vn", "Nguyễn Văn Khách", "Customer", "Customer@123");

        // ----- Danh mục tra cứu -----
        if (!await db.PaymentMethods.AnyAsync())
        {
            db.PaymentMethods.AddRange(
                new PaymentMethod { Name = "Thanh toán khi nhận hàng (COD)" },
                new PaymentMethod { Name = "Chuyển khoản ngân hàng" });
            db.ShippingMethods.AddRange(
                new ShippingMethod { Name = "Giao tiêu chuẩn", Fee = 30000, EstimatedDays = 3 },
                new ShippingMethod { Name = "Giao nhanh", Fee = 50000, EstimatedDays = 1 });
            db.Colors.AddRange(
                new Color { Name = "Đen", HexCode = "#000000" },
                new Color { Name = "Trắng", HexCode = "#FFFFFF" },
                new Color { Name = "Xanh navy", HexCode = "#001F3F" },
                new Color { Name = "Be", HexCode = "#D8C3A5" },
                new Color { Name = "Đỏ đô", HexCode = "#7B1E2B" });
            db.Sizes.AddRange(
                new Size { Name = "S", SortOrder = 1 }, new Size { Name = "M", SortOrder = 2 },
                new Size { Name = "L", SortOrder = 3 }, new Size { Name = "XL", SortOrder = 4 },
                new Size { Name = "Free size", SortOrder = 5 });
            await db.SaveChangesAsync();
        }

        if (!await db.Banners.AnyAsync())
        {
            db.Banners.AddRange(
                new Banner
                {
                    Title = "Bộ sưu tập mới",
                    SortOrder = 1,
                    LinkUrl = "/Product",
                    ImageUrl = "https://placehold.co/1200x400/212529/ffffff?text=BO+SUU+TAP+MOI"
                },
                new Banner
                {
                    Title = "Sale đến 30%",
                    SortOrder = 2,
                    LinkUrl = "/Product?sort=price_asc",
                    ImageUrl = "https://placehold.co/1200x400/842029/ffffff?text=SALE+DEN+30%25"
                },
                new Banner
                {
                    Title = "Miễn phí đổi trả",
                    SortOrder = 3,
                    LinkUrl = "/Product",
                    ImageUrl = "https://placehold.co/1200x400/0f5132/ffffff?text=DOI+TRA+7+NGAY"
                });
            await db.SaveChangesAsync();
        }
        // Phương thức VNPay (thêm riêng để DB cũ cũng có)
        if (!await db.PaymentMethods.AnyAsync(p => p.Name.Contains("VNPay")))
        {
            db.PaymentMethods.Add(new PaymentMethod { Name = "Thanh toán online VNPay (sandbox)" });
            await db.SaveChangesAsync();
        }

        // Mã giảm giá mẫu
        if (!await db.Coupons.AnyAsync())
        {
            db.Coupons.AddRange(
                new Coupon
                {
                    Code = "WELCOME10",
                    DiscountType = DiscountType.Percent,
                    DiscountValue = 10,
                    MinOrderAmount = 200000,
                    MaxUsage = 100,
                    StartDate = DateTime.Today.AddDays(-1),
                    EndDate = DateTime.Today.AddYears(1)
                },
                new Coupon
                {
                    Code = "GIAM50K",
                    DiscountType = DiscountType.FixedAmount,
                    DiscountValue = 50000,
                    MinOrderAmount = 500000,
                    MaxUsage = 50,
                    StartDate = DateTime.Today.AddDays(-1),
                    EndDate = DateTime.Today.AddYears(1)
                });
            await db.SaveChangesAsync();
        }
        await SeedCatalogAsync(db);
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db)
    {
        if (await db.Products.AnyAsync()) return;

        Category Cat(string name, Category? parent = null) =>
            new() { Name = name, Slug = name.ToSlug(), ParentCategory = parent };

        var ao = Cat("Áo"); var quan = Cat("Quần"); var vay = Cat("Váy & Đầm"); var phuKien = Cat("Phụ kiện");
        var aoThun = Cat("Áo thun", ao); var soMi = Cat("Áo sơ mi", ao); var khoac = Cat("Áo khoác", ao);
        var jean = Cat("Quần jean", quan); var quanShort = Cat("Quần short", quan);
        var chanVay = Cat("Chân váy", vay); var dam = Cat("Đầm", vay);
        var tui = Cat("Túi xách", phuKien); var mu = Cat("Mũ nón", phuKien);
        db.Categories.AddRange(ao, quan, vay, phuKien, aoThun, soMi, khoac, jean, quanShort, chanVay, dam, tui, mu);

        Brand B(string n) => new() { Name = n, Slug = n.ToSlug() };
        var urban = B("Urban Basic"); var moc = B("Mộc Studio"); var saigon = B("Saigon Line");
        db.Brands.AddRange(urban, moc, saigon);
        await db.SaveChangesAsync();

        var colors = await db.Colors.ToListAsync();
        var sizes = await db.Sizes.OrderBy(s => s.SortOrder).ToListAsync();
        var clothSizes = sizes.Where(s => s.Name != "Free size").ToList();
        var freeSize = sizes.Where(s => s.Name == "Free size").ToList();

        var items = new (string Name, Category Cat, Brand Brand, decimal Price, int Discount, string Gender, string Material)[]
        {
            ("Áo thun cotton basic", aoThun, urban, 199000, 0, "Unisex", "Cotton 100%"),
            ("Áo thun oversize in hình", aoThun, moc, 259000, 15, "Unisex", "Cotton compact"),
            ("Áo polo nam", aoThun, urban, 299000, 10, "Nam", "Cá sấu cotton"),
            ("Áo sơ mi trắng công sở", soMi, saigon, 389000, 0, "Nam", "Cotton lụa"),
            ("Áo sơ mi linen nữ", soMi, moc, 359000, 20, "Nữ", "Linen"),
            ("Áo khoác bomber", khoac, urban, 599000, 25, "Unisex", "Dù cao cấp"),
            ("Áo khoác denim", khoac, saigon, 649000, 0, "Nam", "Denim"),
            ("Quần jean slim fit", jean, urban, 499000, 10, "Nam", "Denim co giãn"),
            ("Quần jean ống rộng", jean, moc, 529000, 0, "Nữ", "Denim"),
            ("Quần short kaki", quanShort, saigon, 329000, 0, "Nam", "Kaki"),
            ("Chân váy chữ A", chanVay, moc, 289000, 30, "Nữ", "Tuyết mưa"),
            ("Đầm suông hoa nhí", dam, saigon, 459000, 15, "Nữ", "Voan"),
            ("Đầm dạ hội đen", dam, moc, 899000, 0, "Nữ", "Satin"),
            ("Túi tote canvas", tui, urban, 189000, 0, "Unisex", "Canvas"),
            ("Mũ lưỡi trai", mu, urban, 149000, 10, "Unisex", "Kaki"),
        };

        var rnd = new Random(42);
        var skuNo = 1;
        var palette = new[] { "e9ecef/212529", "f8d7da/842029", "d1e7dd/0f5132" };

        foreach (var it in items)
        {
            var p = new Product
            {
                Name = it.Name,
                Slug = it.Name.ToSlug(),
                Description = $"{it.Name} chất liệu {it.Material}, form chuẩn, dễ phối đồ. Thiết kế bởi {it.Brand.Name}.",
                BasePrice = it.Price,
                DiscountPercent = it.Discount,
                Material = it.Material,
                Gender = it.Gender,
                Category = it.Cat,
                Brand = it.Brand,
                CreatedAt = DateTime.Now.AddDays(-rnd.Next(0, 60))
            };

            var text = Uri.EscapeDataString(it.Name.ToSlug().Replace('-', ' '));
            for (int i = 0; i < palette.Length; i++)
                p.Images.Add(new ProductImage
                {
                    ImageUrl = $"https://placehold.co/600x800/{palette[i]}?text={text}",
                    IsMain = i == 0,
                    SortOrder = i
                });

            var useSizes = (it.Cat == tui || it.Cat == mu) ? freeSize : clothSizes;
            foreach (var c in colors.OrderBy(_ => rnd.Next()).Take(3))
                foreach (var s in useSizes)
                    p.Variants.Add(new ProductVariant
                    {
                        Color = c,
                        Size = s,
                        Sku = $"FS{skuNo++:0000}",
                        StockQuantity = rnd.Next(0, 4) == 0 ? 0 : rnd.Next(5, 30)
                    });

            db.Products.Add(p);
        }
        await db.SaveChangesAsync();
    }
}