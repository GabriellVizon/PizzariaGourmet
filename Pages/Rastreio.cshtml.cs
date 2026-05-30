using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PizzariaGourmet.Services;

public class RastreioModel : PageModel
{
    private readonly OrderService _orderSvc;

    public RastreioModel(OrderService orderSvc)
    {
        _orderSvc = orderSvc;
    }

    [BindProperty]
    public string? Telefone { get; set; }

    [BindProperty]
    public string? PedidoId { get; set; }

    public List<OrderTrackingDto> Pedidos { get; set; } = new();
    public bool Buscou { get; set; }

    public record OrderTrackingDto(string Id, string Status, string CreatedAt, decimal Total, string PaymentMethod, string Items);

    public async Task<IActionResult> OnPostAsync()
    {
        Buscou = true;

        if (!string.IsNullOrEmpty(PedidoId))
        {
            var order = await _orderSvc.GetByIdAsync(PedidoId);
            if (order != null)
                Pedidos.Add(new OrderTrackingDto(order.Id, order.Status, order.CreatedAt, order.Total, order.PaymentMethod, order.Items));
        }
        else if (!string.IsNullOrEmpty(Telefone))
        {
            var orders = await _orderSvc.GetByPhoneAsync(Telefone);
            Pedidos = orders.Select(o => new OrderTrackingDto(o.Id, o.Status, o.CreatedAt, o.Total, o.PaymentMethod, o.Items)).ToList();
        }

        return Page();
    }
}