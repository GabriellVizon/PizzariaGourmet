using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PizzariaGourmet.Services;

public class HistoricoModel : PageModel
{
    private readonly OrderService _orderSvc;

    public HistoricoModel(OrderService orderSvc)
    {
        _orderSvc = orderSvc;
    }

    [BindProperty]
    public string? Telefone { get; set; }

    public List<OrderHistoryDto> Pedidos { get; set; } = new();
    public bool Buscou { get; set; }

    public record OrderHistoryDto(string Id, string Status, string CreatedAt, decimal Total, string PaymentMethod, string Items);

    public async Task<IActionResult> OnPostAsync()
    {
        Buscou = true;

        if (!string.IsNullOrEmpty(Telefone))
        {
            var orders = await _orderSvc.GetByPhoneAsync(Telefone);
            Pedidos = orders.Select(o => new OrderHistoryDto(o.Id, o.Status, o.CreatedAt, o.Total, o.PaymentMethod, o.Items)).ToList();
        }

        return Page();
    }
}
