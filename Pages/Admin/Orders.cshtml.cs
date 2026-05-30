using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PizzariaGourmet.Services;

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

    public async Task OnGetAsync()
    {
        var orders = await _orderSvc.GetAllAsync();
        Orders = orders.Select(o => new OrderDto(o.Id, o.CreatedAt, o.Status, o.CustomerName, o.Total, o.PaymentMethod)).ToList();
    }
}
