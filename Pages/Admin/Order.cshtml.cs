using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PizzariaGourmet.Services;

[Authorize]
public class OrderModel : PageModel
{
    private readonly OrderService _orderSvc;

    public OrderModel(OrderService orderSvc)
    {
        _orderSvc = orderSvc;
    }

    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; }

    public string CreatedAt { get; set; } = "";
    public string Status { get; set; } = "";
    public string Payload { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public string CustomerCPF { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Admin/Orders");

        var order = await _orderSvc.GetByIdAsync(Id);
        if (order == null) return NotFound();

        Id = order.Id;
        CreatedAt = order.CreatedAt;
        Status = order.Status;
        Payload = order.Items;
        CustomerName = order.CustomerName;
        CustomerPhone = order.CustomerPhone;
        CustomerAddress = order.CustomerAddress;
        CustomerCPF = order.CustomerCPF;
        Subtotal = order.Subtotal;
        DeliveryFee = order.DeliveryFee;
        Total = order.Total;
        PaymentMethod = order.PaymentMethod;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync([FromForm] string status, [FromQuery] int? delete)
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Admin/Orders");

        if (delete == 1)
        {
            await _orderSvc.DeleteAsync(Id);
            return RedirectToPage("/Admin/Orders");
        }

        await _orderSvc.UpdateStatusAsync(Id, status);

        if (status == "paid")
        {
            var order = await _orderSvc.GetByIdAsync(Id);
            if (order != null)
            {
                var notifySvc = HttpContext.RequestServices.GetRequiredService<NotificationService>();
                await notifySvc.SendNotificationsAsync(Id, order.Items);
            }
        }

        return RedirectToPage(new { id = Id });
    }
}
