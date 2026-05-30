using MailKit.Net.Smtp;
using MimeKit;

namespace PizzariaGourmet.Services;

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
        return $"{domain}/Rastreio?PedidoId={orderId}";
    }

    public async Task SendNotificationsAsync(string orderId, string payload)
    {
        _logger.LogInformation("Sending notifications for order {OrderId}", orderId);
        var trackingUrl = GetTrackingUrl(orderId);
        await SendEmailAsync(orderId, payload + $"\n\nAcompanhe seu pedido: {trackingUrl}");
        await SendSmsAsync(orderId, payload);
    }
}
