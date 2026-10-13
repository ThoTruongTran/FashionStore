// File: DashboardController.cs  (thay bản tạm)
using ClosedXML.Excel;
namespace FashionStore.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var from7 = today.AddDays(-6);

        var completed = _db.Orders.Where(o => o.Status == OrderStatus.Completed);
        var byStatus = await _db.Orders.GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        var recent7 = await _db.Orders.Where(o => o.Status != OrderStatus.Cancelled && o.CreatedAt >= from7)
            .Select(o => new { o.CreatedAt, o.TotalAmount }).ToListAsync();
        var from12 = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
        var monthly = await completed.Where(o => o.CreatedAt >= from12)
            .Select(o => new { o.CreatedAt, o.TotalAmount }).ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalRevenue = await completed.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            MonthRevenue = await completed.Where(o => o.CreatedAt >= monthStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            TotalOrders = byStatus.Sum(x => x.Count),
            PendingOrders = byStatus.FirstOrDefault(x => x.Status == OrderStatus.Pending)?.Count ?? 0,
            TotalProducts = await _db.Products.CountAsync(),
            TotalCustomers = await _db.Users.CountAsync(u => u.Role!.Name == "Customer"),
            OrdersByStatus = Enum.GetValues<OrderStatus>().Select(s => new NameValue
            {
                Name = s.ToLabel(),
                Value = byStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0
            }).ToList(),
            Last7Days = Enumerable.Range(0, 7).Select(i =>
            {
                var d = from7.AddDays(i);
                var day = recent7.Where(o => o.CreatedAt.Date == d).ToList();
                return new DayRevenue { Date = d, Revenue = day.Sum(o => o.TotalAmount), Orders = day.Count };
            }).ToList(),
            TopProducts = await _db.OrderDetails.Where(d => d.Order!.Status != OrderStatus.Cancelled)
                .GroupBy(d => d.ProductVariant!.Product!.Name)
                .Select(g => new NameValue { Name = g.Key, Value = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Value).Take(5).ToListAsync(),
            LowStock = await _db.ProductVariants.Include(v => v.Product).Include(v => v.Color).Include(v => v.Size)
                .Where(v => v.StockQuantity <= 5 && v.Product!.IsActive)
                .OrderBy(v => v.StockQuantity).Take(8).ToListAsync(),
            MonthlyRevenue = Enumerable.Range(0, 12).Select(i =>
            {
                var m = from12.AddMonths(i);
                return new NameValue
                {
                    Name = m.ToString("MM/yyyy"),
                    Value = (long)monthly.Where(o => o.CreatedAt.Year == m.Year && o.CreatedAt.Month == m.Month).Sum(o => o.TotalAmount)
                };
            }).ToList(),
            RecentOrders = await _db.Orders.OrderByDescending(o => o.CreatedAt).Take(8).ToListAsync()
        };
        return View(vm);
    }
    public async Task<IActionResult> ExportRevenue(int? year)
    {
        var y = year ?? DateTime.Today.Year;
        var orders = await _db.Orders.Where(o => o.Status == OrderStatus.Completed && o.CreatedAt.Year == y)
            .Select(o => new { o.CreatedAt, o.SubTotal, o.ShippingFee, o.DiscountAmount, o.TotalAmount }).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Doanh thu {y}");
        string[] heads = { "Tháng", "Số đơn", "Tạm tính", "Phí vận chuyển", "Giảm giá", "Doanh thu" };
        for (int c = 0; c < heads.Length; c++) ws.Cell(1, c + 1).Value = heads[c];
        for (int m = 1; m <= 12; m++)
        {
            var g = orders.Where(o => o.CreatedAt.Month == m).ToList(); var r = m + 1;
            ws.Cell(r, 1).Value = $"Tháng {m}";
            ws.Cell(r, 2).Value = g.Count;
            ws.Cell(r, 3).Value = g.Sum(o => o.SubTotal);
            ws.Cell(r, 4).Value = g.Sum(o => o.ShippingFee);
            ws.Cell(r, 5).Value = g.Sum(o => o.DiscountAmount);
            ws.Cell(r, 6).Value = g.Sum(o => o.TotalAmount);
        }
        ws.Cell(14, 1).Value = "Tổng cộng";
        foreach (var col in new[] { "B", "C", "D", "E", "F" }) ws.Cell($"{col}14").FormulaA1 = $"SUM({col}2:{col}13)";
        ws.Range(2, 3, 14, 6).Style.NumberFormat.Format = "#,##0";
        ws.Row(1).Style.Font.Bold = true; ws.Row(14).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"doanh-thu-{y}.xlsx");
    }
}