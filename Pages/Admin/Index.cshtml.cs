using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomPizzaria.Services;

[Authorize]
public class AdminDashboardModel : PageModel
{
    private readonly OrderService _orderSvc;

    public AdminDashboardModel(OrderService orderSvc)
    {
        _orderSvc = orderSvc;
    }

    public int PendingOrders { get; set; }
    public int OrdersToday { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal TotalRevenue { get; set; }

    public async Task OnGetAsync()
    {
        var orders = await _orderSvc.GetAllAsync();
        var today = DateTime.UtcNow.Date;

        PendingOrders = orders.Count(o => o.Status == "pending" || o.Status == "paid" || o.Status == "preparing");
        OrdersToday = orders.Count(o => o.CreatedAt.StartsWith(today.ToString("yyyy-MM-dd")));
        RevenueToday = orders.Where(o => o.CreatedAt.StartsWith(today.ToString("yyyy-MM-dd")) && o.Status != "cancelled").Sum(o => o.Total);
        TotalRevenue = orders.Where(o => o.Status != "cancelled").Sum(o => o.Total);
    }
}