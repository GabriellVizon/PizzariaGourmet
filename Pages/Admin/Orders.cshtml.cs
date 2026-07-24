using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomPizzaria.Services;

[Authorize]
public class OrdersModel : PageModel
{
    private readonly OrderService _orderSvc;

    public OrdersModel(OrderService orderSvc)
    {
        _orderSvc = orderSvc;
    }

    public record OrderDto(string Id, string CreatedAt, string Status, string CustomerName, decimal Total, string PaymentMethod);

    public List<OrderDto> Orders { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Name { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Phone { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    public async Task OnGetAsync()
    {
        var orders = await _orderSvc.SearchAsync(Name, Phone, Status, DateFrom, DateTo);
        Orders = orders.Select(o => new OrderDto(o.Id, o.CreatedAt, o.Status, o.CustomerName, o.Total, o.PaymentMethod)).ToList();
    }
}
