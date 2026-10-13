// File: ProductViewModels.cs   (gồm cả HomeViewModel và PagingInfo cho gọn, tách file nếu muốn)
namespace FashionStore.Models.ViewModels;

public class PagingInfo { public int Page { get; set; } = 1; public int TotalPages { get; set; } = 1; }

public class HomeViewModel
{
    public List<Banner> Banners { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Product> NewProducts { get; set; } = new();
    public List<Product> SaleProducts { get; set; } = new();
}

public class ProductFilter
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? Gender { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
}

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public ProductFilter Filter { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public PagingInfo Paging { get; set; } = new();
    public int TotalCount { get; set; }
}

public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;
    public List<Product> Related { get; set; } = new();
    public List<Color> Colors { get; set; } = new();
    public List<Size> Sizes { get; set; } = new();
    public double AvgRating { get; set; }
}