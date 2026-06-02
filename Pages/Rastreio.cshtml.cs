using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PizzariaGourmet.Services;

public class RastreioModel : PageModel
{
    private readonly OrderService _orderSvc;
    private readonly NotificationService _notifySvc;
    private readonly WhatsAppService _whatsAppSvc;

    public RastreioModel(OrderService orderSvc, NotificationService notifySvc, WhatsAppService whatsAppSvc)
    {
        _orderSvc = orderSvc;
        _notifySvc = notifySvc;
        _whatsAppSvc = whatsAppSvc;
    }

    [BindProperty]
    public string? Telefone { get; set; }

    [BindProperty]
    public string? PedidoId { get; set; }

    public List<OrderTrackingDto> Pedidos { get; set; } = new();
    public bool Buscou { get; set; }
    public string? CancelResult { get; set; }

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

    public async Task<IActionResult> OnPostCancelAsync(string orderId, string telefone, string pedidoId)
    {
        var order = await _orderSvc.GetByIdAsync(orderId);
        if (order == null)
            return RedirectToPage(new { error = "Pedido não encontrado" });

        if (order.Status != "pending" && order.Status != "paid")
            return RedirectToPage(new { error = "Pedido não pode mais ser cancelado" });

        await _orderSvc.UpdateStatusAsync(orderId, "cancelled");

        // Notify admin
        await _notifySvc.SendNotificationsAsync(orderId, order.Items);

        // Notify customer via WhatsApp
        if (!string.IsNullOrEmpty(order.CustomerPhone))
        {
            var domain = Environment.GetEnvironmentVariable("DOMAIN") ?? "http://localhost:5000";
            await _whatsAppSvc.SendStatusUpdateAsync(order.CustomerPhone, order.CustomerName, orderId, "cancelled", domain);
        }

        Telefone = telefone;
        PedidoId = pedidoId;
        CancelResult = "Pedido cancelado com sucesso!";
        Buscou = true;

        // Reload orders
        if (!string.IsNullOrEmpty(PedidoId))
        {
            var updatedOrder = await _orderSvc.GetByIdAsync(PedidoId);
            if (updatedOrder != null)
                Pedidos.Add(new OrderTrackingDto(updatedOrder.Id, updatedOrder.Status, updatedOrder.CreatedAt, updatedOrder.Total, updatedOrder.PaymentMethod, updatedOrder.Items));
        }
        else if (!string.IsNullOrEmpty(Telefone))
        {
            var orders = await _orderSvc.GetByPhoneAsync(Telefone);
            Pedidos = orders.Select(o => new OrderTrackingDto(o.Id, o.Status, o.CreatedAt, o.Total, o.PaymentMethod, o.Items)).ToList();
        }

        return Page();
    }
}