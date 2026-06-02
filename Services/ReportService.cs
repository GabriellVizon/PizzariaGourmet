using Microsoft.EntityFrameworkCore;
using PizzariaGourmet.Data;
using PizzariaGourmet.Models;

namespace PizzariaGourmet.Services;

public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public record SalesReport(
        decimal TotalRevenue,
        int TotalOrders,
        decimal AvgOrderValue,
        int CancelledOrders,
        decimal TotalDiscounts,
        List<DailySales> DailySales,
        List<ProductSales> TopProducts,
        List<PaymentMethodSales> PaymentBreakdown
    );

    public record DailySales(string Date, int Orders, decimal Revenue);
    public record ProductSales(string Name, int Quantity, decimal Revenue);
    public record PaymentMethodSales(string Method, int Orders, decimal Revenue);

    public async Task<SalesReport> GetReportAsync(DateTime? dateFrom, DateTime? dateTo)
    {
        var orders = await _db.Orders.ToListAsync();
        var filtered = orders.AsEnumerable();

        if (dateFrom.HasValue)
            filtered = filtered.Where(o => DateTime.TryParse(o.CreatedAt, out var dt) && dt >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            filtered = filtered.Where(o => DateTime.TryParse(o.CreatedAt, out var dt) && dt < dateTo.Value.Date.AddDays(1));

        var list = filtered.ToList();
        var active = list.Where(o => o.Status != "cancelled").ToList();
        var cancelled = list.Where(o => o.Status == "cancelled").ToList();

        var dailySales = list
            .GroupBy(o =>
            {
                DateTime.TryParse(o.CreatedAt, out var dt);
                return dt.Date;
            })
            .Select(g => new DailySales(
                g.Key.ToString("yyyy-MM-dd"),
                g.Count(),
                g.Where(o => o.Status != "cancelled").Sum(o => o.Total)
            ))
            .OrderBy(d => d.Date)
            .ToList();

        var topProducts = new List<ProductSales>();
        var productCounts = new Dictionary<string, (int Qty, decimal Rev)>();
        foreach (var order in active)
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(order.Items);
                var root = doc.RootElement;
                var items = root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty("cart", out var cart)
                    ? cart.EnumerateArray() : root.EnumerateArray();
                foreach (var item in items)
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";
                    var qty = item.TryGetProperty("qty", out var q) && q.TryGetInt32(out var qi) ? qi : 1;
                    var price = item.TryGetProperty("price", out var p) && p.TryGetDecimal(out var pd) ? pd : 0;
                    var compsPrice = 0m;
                    if (item.TryGetProperty("complements", out var comps) && comps.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var c in comps.EnumerateArray())
                            if (c.TryGetProperty("price", out var cp) && cp.TryGetDecimal(out var cpd))
                                compsPrice += cpd;
                    }
                    var totalItem = (price + compsPrice) * qty;
                    if (productCounts.ContainsKey(name))
                        productCounts[name] = (productCounts[name].Qty + qty, productCounts[name].Rev + totalItem);
                    else
                        productCounts[name] = (qty, totalItem);
                }
            }
            catch { }
        }
        topProducts = productCounts
            .Select(kv => new ProductSales(kv.Key, kv.Value.Qty, kv.Value.Rev))
            .OrderByDescending(p => p.Quantity)
            .Take(10)
            .ToList();

        var paymentBreakdown = active
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new PaymentMethodSales(g.Key, g.Count(), g.Sum(o => o.Total)))
            .ToList();

        return new SalesReport(
            active.Sum(o => o.Total),
            active.Count,
            active.Count > 0 ? active.Sum(o => o.Total) / active.Count : 0,
            cancelled.Count,
            list.Sum(o => o.Discount),
            dailySales,
            topProducts,
            paymentBreakdown
        );
    }
}
