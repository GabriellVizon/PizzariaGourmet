using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomPizzaria.Services;

[Authorize]
[ValidateAntiForgeryToken]
public class OrderModel : PageModel
{
    private readonly OrderService _orderSvc;
    private readonly NotificationService _notifySvc;
    private readonly WhatsAppService _whatsAppSvc;

    public OrderModel(OrderService orderSvc, NotificationService notifySvc, WhatsAppService whatsAppSvc)
    {
        _orderSvc = orderSvc;
        _notifySvc = notifySvc;
        _whatsAppSvc = whatsAppSvc;
    }

    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; }

    public string CreatedAt { get; set; } = "";
    public string Status { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public string CustomerCPF { get; set; } = "";
    public string CustomerNotes { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? CouponCode { get; set; }
    public decimal Discount { get; set; }
    public string? ScheduledTime { get; set; }
    public string? DeliveryPersonName { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();

    public class OrderItemDto
    {
        public string Name { get; set; } = "";
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public string Size { get; set; } = "";
        public string Complements { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Admin/Orders");

        var order = await _orderSvc.GetByIdAsync(Id);
        if (order == null) return NotFound();

        Id = order.Id;
        CreatedAt = order.CreatedAt;
        Status = order.Status;
        CustomerName = order.CustomerName;
        CustomerPhone = order.CustomerPhone;
        CustomerEmail = order.CustomerEmail;
        CustomerAddress = order.CustomerAddress;
        CustomerCPF = order.CustomerCPF;
        CustomerNotes = order.CustomerNotes;
        ScheduledTime = order.ScheduledTime;
        DeliveryPersonName = order.DeliveryPersonName;
        Subtotal = order.Subtotal;
        DeliveryFee = order.DeliveryFee;
        Total = order.Total;
        PaymentMethod = order.PaymentMethod;
        CouponCode = order.CouponCode;
        Discount = order.Discount;
        Items = ParseItems(order.Items);

        return Page();
    }

    private List<OrderItemDto> ParseItems(string itemsJson)
    {
        try
        {
            var doc = JsonDocument.Parse(itemsJson);
            var root = doc.RootElement;

            JsonElement.ArrayEnumerator itemsEnum;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("cart", out var cartEl))
                itemsEnum = cartEl.EnumerateArray();
            else if (root.ValueKind == JsonValueKind.Array)
                itemsEnum = root.EnumerateArray();
            else
                return new List<OrderItemDto>();

            var list = new List<OrderItemDto>();
            foreach (var item in itemsEnum)
            {
                var dto = new OrderItemDto
                {
                    Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "Produto",
                    Qty = item.TryGetProperty("qty", out var q) && q.TryGetInt32(out var qi) ? qi : 1,
                    Price = item.TryGetProperty("price", out var p) && p.TryGetDecimal(out var pd) ? pd : 0,
                    Size = item.TryGetProperty("size", out var s) && s.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : ""
                };

                if (item.TryGetProperty("complements", out var compsEl) && compsEl.ValueKind == JsonValueKind.Array)
                {
                    var compNames = new List<string>();
                    foreach (var comp in compsEl.EnumerateArray())
                    {
                        if (comp.TryGetProperty("name", out var cn))
                            compNames.Add(cn.GetString() ?? "");
                    }
                    dto.Complements = string.Join(", ", compNames);
                }

                list.Add(dto);
            }
            return list;
        }
        catch
        {
            return new List<OrderItemDto>();
        }
    }

    public async Task<IActionResult> OnPostAsync([FromForm] string status, [FromQuery] int? delete)
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Admin/Orders");

        if (delete == 1)
        {
            await _orderSvc.DeleteAsync(Id);
            return RedirectToPage("/Admin/Orders");
        }

        var order = await _orderSvc.GetByIdAsync(Id);
        if (order == null) return NotFound();

        var oldStatus = order.Status;
        await _orderSvc.UpdateStatusAsync(Id, status);

        if ((status == "paid" && oldStatus != "paid") || (status == "cancelled" && oldStatus != "cancelled"))
            await _notifySvc.SendNotificationsAsync(Id, order.Items);

        // Send WhatsApp to customer on any status change
        if (!string.IsNullOrEmpty(order.CustomerPhone) && status != oldStatus)
        {
            var domain = Environment.GetEnvironmentVariable("DOMAIN") ?? "http://localhost:5000";
            await _whatsAppSvc.SendStatusUpdateAsync(order.CustomerPhone, order.CustomerName, Id, status, domain);
        }

        return RedirectToPage(new { id = Id });
    }
}
