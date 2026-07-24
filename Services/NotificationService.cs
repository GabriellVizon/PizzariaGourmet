using MailKit.Net.Smtp;
using MimeKit;

namespace DomPizzaria.Services;

public class NotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IConfiguration config, ILogger<NotificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string orderId, string payload)
    {
        var smtpHost = _config["SMTP_HOST"];
        var smtpPortStr = _config["SMTP_PORT"];
        var smtpUser = _config["SMTP_USER"];
        var smtpPass = _config["SMTP_PASS"];
        var toEmail = _config["NOTIFY_EMAIL_TO"];

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(toEmail))
        {
            _logger.LogWarning("SMTP not configured - skipping email notification for order {OrderId}", orderId);
            return;
        }

        var smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(smtpUser ?? "noreply@localhost"));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"Novo pedido recebido — {orderId}";
            message.Body = new TextPart("plain")
            {
                Text = $"Pedido {orderId} confirmado.\n\nDetalhes:\n{payload}"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
                await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for order {OrderId}", orderId);
        }
    }

    public async Task SendSmsAsync(string orderId, string payload)
    {
        var twilioSid = _config["TWILIO_ACCOUNT_SID"];
        var twilioToken = _config["TWILIO_AUTH_TOKEN"];
        var twilioFrom = _config["TWILIO_FROM"];
        var notifyPhone = _config["NOTIFY_PHONE_TO"];

        if (string.IsNullOrEmpty(twilioSid) || string.IsNullOrEmpty(twilioToken) ||
            string.IsNullOrEmpty(twilioFrom) || string.IsNullOrEmpty(notifyPhone))
        {
            _logger.LogWarning("Twilio not configured - skipping SMS notification for order {OrderId}", orderId);
            return;
        }

        try
        {
            using var httpClient = new System.Net.Http.HttpClient();
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{twilioSid}/Messages.json";
            var form = new List<KeyValuePair<string, string>>
            {
                new("To", notifyPhone),
                new("From", twilioFrom),
                new("Body", $"Pedido {orderId} confirmado. Detalhes: {payload[..Math.Min(200, payload.Length)]}")
            };
            var reqMsg = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url)
            {
                Content = new System.Net.Http.FormUrlEncodedContent(form)
            };
            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{twilioSid}:{twilioToken}");
            reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(byteArray));
            await httpClient.SendAsync(reqMsg);
            _logger.LogInformation("SMS notification sent for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS notification for order {OrderId}", orderId);
        }
    }

    public string GetTrackingUrl(string orderId)
    {
        var domain = Environment.GetEnvironmentVariable("DOMAIN") ?? "http://localhost:5000";
        return $"{domain}/Admin/Order?id={orderId}";
    }

    public async Task SendCustomerConfirmationAsync(string customerEmail, string customerName, string orderId, string orderSummary)
    {
        if (string.IsNullOrEmpty(customerEmail))
        {
            _logger.LogWarning("Customer email is empty — skipping confirmation for order {OrderId}", orderId);
            return;
        }

        var smtpHost = _config["SMTP_HOST"];
        var smtpPortStr = _config["SMTP_PORT"];
        var smtpUser = _config["SMTP_USER"];
        var smtpPass = _config["SMTP_PASS"];

        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("SMTP not configured — skipping customer confirmation email for order {OrderId}", orderId);
            return;
        }

        var smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;
        var domain = Environment.GetEnvironmentVariable("DOMAIN") ?? "http://localhost:5000";
        var trackingUrl = $"{domain}/Admin/Order?id={orderId}";
        var shortId = orderId.Length > 8 ? orderId[..8] : orderId;

        try
        {
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px'>
<div style='background:#e74c3c;color:#fff;padding:24px;border-radius:12px 12px 0 0;text-align:center'>
<h1 style='margin:0'>🍕 Pedido Confirmado!</h1>
</div>
<div style='background:#fff;border:1px solid #eee;padding:24px;border-radius:0 0 12px 12px'>
<p>Olá <strong>{customerName}</strong>,</p>
<p>Seu pedido foi recebido com sucesso.</p>
<div style='background:#f8f8f8;border-radius:8px;padding:16px;margin:16px 0;text-align:center'>
<div style='font-size:0.85rem;color:#888'>Nº do Pedido</div>
<div style='font-size:1.5rem;font-weight:800;color:#e74c3c'>#{shortId}</div>
</div>
{orderSummary}
<div style='margin:20px 0;text-align:center'>
<a href='{trackingUrl}' style='display:inline-block;background:#e74c3c;color:#fff;padding:12px 24px;border-radius:50px;text-decoration:none;font-weight:700'>📦 Acompanhar Pedido</a>
</div>
<p style='color:#888;font-size:0.85rem'>A equipe da <strong>Dom Pizzaria</strong> vai preparar tudo com carinho. 🍕</p>
</div>
</body>
</html>",
                TextBody = $"Pedido #{shortId} confirmado!\n\nAcompanhe pelo link: {trackingUrl}"
            };

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(smtpUser ?? "noreply@dompizzaria.com"));
            message.To.Add(MailboxAddress.Parse(customerEmail));
            message.Subject = $"🍕 Pedido #{shortId} confirmado — Dom Pizzaria";
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
                await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Customer confirmation email sent to {Email} for order {OrderId}", customerEmail, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send customer confirmation email for order {OrderId} to {Email}", orderId, customerEmail);
        }
    }

    public async Task SendNotificationsAsync(string orderId, string payload)
    {
        _logger.LogInformation("Sending notifications for order {OrderId}", orderId);
        var trackingUrl = GetTrackingUrl(orderId);
        await SendEmailAsync(orderId, payload + $"\n\nAcompanhe seu pedido: {trackingUrl}");
        await SendSmsAsync(orderId, payload);
    }
}
