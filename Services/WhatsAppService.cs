using System.Text;
using System.Text.Json;

namespace DomPizzaria.Services;

public class WhatsAppService
{
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(ILogger<WhatsAppService> logger)
    {
        _logger = logger;
    }

    public async Task SendStatusUpdateAsync(string customerPhone, string customerName, string orderId, string status, string domain)
    {
        var apiUrl = Environment.GetEnvironmentVariable("WHATSAPP_API_URL");
        var apiKey = Environment.GetEnvironmentVariable("WHATSAPP_API_KEY");

        if (string.IsNullOrEmpty(apiUrl))
        {
            _logger.LogWarning("WHATSAPP_API_URL not configured - skipping WhatsApp notification for order {OrderId}", orderId);
            return;
        }

        var statusLabels = new Dictionary<string, string>
        {
            ["pending"] = "Pendente",
            ["paid"] = "Pago",
            ["preparing"] = "Preparando",
            ["delivering"] = "Saiu para entrega",
            ["delivered"] = "Entregue",
            ["cancelled"] = "Cancelado"
        };

        var statusEmojis = new Dictionary<string, string>
        {
            ["pending"] = "📋",
            ["paid"] = "✅",
            ["preparing"] = "🍕",
            ["delivering"] = "🚚",
            ["delivered"] = "📦",
            ["cancelled"] = "❌"
        };

        var label = statusLabels.GetValueOrDefault(status, status);
        var emoji = statusEmojis.GetValueOrDefault(status, "");
        var trackingUrl = $"{domain}/Admin/Order?id={orderId}";

        var message = $"{emoji} *Atualização do Pedido #{orderId[..8]}*\n\nOlá {customerName}, seu pedido foi atualizado para: *{label}*\n\nAcompanhe pelo link: {trackingUrl}";

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var payload = new
            {
                phone = customerPhone,
                message = message,
                apiKey = apiKey
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp notification sent for order {OrderId} to {Phone}", orderId, customerPhone);
            }
            else
            {
                _logger.LogWarning("WhatsApp API returned {Status} for order {OrderId}: {Body}", response.StatusCode, orderId, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp notification for order {OrderId} to {Phone}", orderId, customerPhone);
        }
    }
}
