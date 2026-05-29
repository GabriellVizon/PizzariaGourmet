using System.Text.Json;
using Stripe;
using Stripe.Checkout;
using Microsoft.Data.Sqlite;
using MailKit.Net.Smtp;
using MimeKit;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

// Produtos (arquivo JSON estático)
// Products CRUD (file-backed)
record Product(int Id, string Name, string Description, decimal Price, string Image);

static string ProductsPath() => Path.Combine(AppContext.BaseDirectory, "Data", "products.json");

static async Task<List<Product>> ReadProductsAsync()
{
    var path = ProductsPath();
    if (!System.IO.File.Exists(path)) return new List<Product>();
    var json = await System.IO.File.ReadAllTextAsync(path);
    try { return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>(); } catch { return new List<Product>(); }
}

static async Task WriteProductsAsync(List<Product> list)
{
    Directory.CreateDirectory(Path.GetDirectoryName(ProductsPath())!);
    var opts = new JsonSerializerOptions { WriteIndented = true };
    await System.IO.File.WriteAllTextAsync(ProductsPath(), JsonSerializer.Serialize(list, opts));
}

app.MapGet("/api/products", async () =>
{
    var list = await ReadProductsAsync();
    return Results.Json(list);
});

app.MapGet("/api/products/{id}", async (int id) =>
{
    var list = await ReadProductsAsync();
    var p = list.FirstOrDefault(x => x.Id == id);
    return p is null ? Results.NotFound() : Results.Json(p);
});

app.MapPost("/api/products", async (HttpRequest req) =>
{
    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
    var name = doc.RootElement.GetProperty("name").GetString() ?? "Produto";
    var desc = doc.RootElement.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
    var price = doc.RootElement.TryGetProperty("price", out var p) && p.TryGetDecimal(out var pd) ? pd : 0m;
    var image = doc.RootElement.TryGetProperty("image", out var im) ? im.GetString() ?? "" : "";

    var list = await ReadProductsAsync();
    var nextId = list.Any() ? list.Max(x => x.Id) + 1 : 1;
    var prod = new Product(nextId, name, desc, price, image);
    list.Add(prod);
    await WriteProductsAsync(list);
    return Results.Created($"/api/products/{prod.Id}", prod);
});

app.MapPut("/api/products/{id}", async (int id, HttpRequest req) =>
{
    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
    var list = await ReadProductsAsync();
    var idx = list.FindIndex(x => x.Id == id);
    if (idx < 0) return Results.NotFound();
    var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? list[idx].Name : list[idx].Name;
    var desc = doc.RootElement.TryGetProperty("description", out var d) ? d.GetString() ?? list[idx].Description : list[idx].Description;
    var price = doc.RootElement.TryGetProperty("price", out var p) && p.TryGetDecimal(out var pd) ? pd : list[idx].Price;
    var image = doc.RootElement.TryGetProperty("image", out var im) ? im.GetString() ?? list[idx].Image : list[idx].Image;
    list[idx] = new Product(id, name, desc, price, image);
    await WriteProductsAsync(list);
    return Results.Ok(list[idx]);
});

app.MapDelete("/api/products/{id}", async (int id) =>
{
    var list = await ReadProductsAsync();
    var idx = list.FindIndex(x => x.Id == id);
    if (idx < 0) return Results.NotFound();
    list.RemoveAt(idx);
    await WriteProductsAsync(list);
    return Results.Ok();
});

// Endpoint de checkout simples (mock) mantido para compatibilidade
app.MapPost("/api/checkout", async (HttpRequest req) =>
{
    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    return Results.Json(new { success = true, message = "Pagamento mock recebido" });
});

// Integração com Stripe: criar sessão de checkout
app.MapPost("/create-checkout-session", async (HttpRequest req) =>
{
    var stripeKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");
    var domain = Environment.GetEnvironmentVariable("DOMAIN") ?? "http://localhost:5000";
    if (string.IsNullOrEmpty(stripeKey))
        return Results.BadRequest(new { error = "STRIPE_API_KEY not configured" });

    StripeConfiguration.ApiKey = stripeKey;

    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);

    if (!doc.RootElement.TryGetProperty("cart", out var cartEl) || cartEl.ValueKind != JsonValueKind.Array)
        return Results.BadRequest(new { error = "Cart missing" });

    // Criar um orderId e salvar o pedido temporariamente no SQLite para reconciliar depois no webhook
    var orderId = Guid.NewGuid().ToString();
    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Data"));
    var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "app.db");
    using (var conn = new SqliteConnection($"Data Source={dbPath}"))
    {
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Orders (
            Id TEXT PRIMARY KEY,
            CreatedAt TEXT,
            Status TEXT,
            Payload TEXT
        );";
        await cmd.ExecuteNonQueryAsync();

        var insert = conn.CreateCommand();
        insert.CommandText = "INSERT INTO Orders (Id, CreatedAt, Status, Payload) VALUES ($id, $created, $status, $payload);";
        insert.Parameters.AddWithValue("$id", orderId);
        insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
        insert.Parameters.AddWithValue("$status", "pending");
        insert.Parameters.AddWithValue("$payload", body);
        await insert.ExecuteNonQueryAsync();
    }

    var options = new SessionCreateOptions
    {
        PaymentMethodTypes = new List<string> { "card" },
        Mode = "payment",
        SuccessUrl = domain + "/Success?session_id={CHECKOUT_SESSION_ID}",
        CancelUrl = domain + "/Checkout",
        LineItems = new List<SessionLineItemOptions>(),
        ClientReferenceId = orderId
    };

    foreach (var item in cartEl.EnumerateArray())
    {
        var name = item.GetProperty("name").GetString();
        var price = item.GetProperty("price").GetDecimal();
        var qty = item.GetProperty("qty").GetInt32();

        options.LineItems.Add(new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = (long)(price * 100), // em centavos
                Currency = "brl",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = name
                }
            },
            Quantity = qty
        });
    }

    var service = new SessionService();
    var session = await service.CreateAsync(options);

    return Results.Json(new { url = session.Url, id = session.Id, orderId });
});

// Endpoint para recuperar sessão (para a página de sucesso)
app.MapGet("/session/{id}", async (string id) =>
{
    var stripeKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");
    if (string.IsNullOrEmpty(stripeKey))
        return Results.BadRequest(new { error = "STRIPE_API_KEY not configured" });
    StripeConfiguration.ApiKey = stripeKey;
    var service = new SessionService();
    try
    {
        var session = await service.GetAsync(id);
        return Results.Json(session);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Webhook do Stripe — verifique STRIPE_WEBHOOK_SECRET nas variáveis de ambiente
app.MapPost("/webhook", async (HttpRequest req) =>
{
    var json = await new StreamReader(req.Body).ReadToEndAsync();
    var sigHeader = req.Headers["Stripe-Signature"].FirstOrDefault();
    var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
    if (string.IsNullOrEmpty(webhookSecret))
    {
        // Se não configurado, apenas aceite o evento (útil para desenvolvimento)
        return Results.Ok();
    }

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(json, sigHeader, webhookSecret);
        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            var orderId = session.ClientReferenceId;
            if (!string.IsNullOrEmpty(orderId))
            {
                var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "app.db");
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                await conn.OpenAsync();
                var update = conn.CreateCommand();
                update.CommandText = "UPDATE Orders SET Status = $status WHERE Id = $id;";
                update.Parameters.AddWithValue("$status", "paid");
                update.Parameters.AddWithValue("$id", orderId);
                await update.ExecuteNonQueryAsync();

                // Ler payload para enviar notificação por e-mail
                var select = conn.CreateCommand();
                select.CommandText = "SELECT Payload FROM Orders WHERE Id = $id;";
                select.Parameters.AddWithValue("$id", orderId);
                var payload = (string?)await select.ExecuteScalarAsync();
                if (!string.IsNullOrEmpty(payload))
                {
                    // Enviar e-mail de notificação
                    var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
                    var smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 587;
                    var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
                    var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS");
                    var toEmail = Environment.GetEnvironmentVariable("NOTIFY_EMAIL_TO");
                    if (!string.IsNullOrEmpty(smtpHost) && !string.IsNullOrEmpty(toEmail))
                    {
                        try
                        {
                            var message = new MimeMessage();
                            message.From.Add(MailboxAddress.Parse(smtpUser ?? "noreply@localhost"));
                            message.To.Add(MailboxAddress.Parse(toEmail));
                            message.Subject = $"Novo pedido recebido — {orderId}";
                            message.Body = new TextPart("plain") { Text = $"Pedido {orderId} confirmado.\n\nDetalhes:\n{payload}" };

                            using var client = new SmtpClient();
                            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
                                await client.AuthenticateAsync(smtpUser, smtpPass);
                            await client.SendAsync(message);
                            await client.DisconnectAsync(true);
                        }
                        catch
                        {
                            // falha no envio do e-mail não bloqueia o webhook
                        }
                    // Enviar SMS/WhatsApp via Twilio, se configurado
                    var twilioSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
                    var twilioToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
                    var twilioFrom = Environment.GetEnvironmentVariable("TWILIO_FROM");
                    var notifyPhone = Environment.GetEnvironmentVariable("NOTIFY_PHONE_TO");
                    if (!string.IsNullOrEmpty(twilioSid) && !string.IsNullOrEmpty(twilioToken) && !string.IsNullOrEmpty(twilioFrom) && !string.IsNullOrEmpty(notifyPhone))
                    {
                        try
                        {
                            var clientTw = new System.Net.Http.HttpClient();
                            var accountSid = twilioSid;
                            var authToken = twilioToken;
                            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                            var form = new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string,string>("To", notifyPhone),
                                new KeyValuePair<string,string>("From", twilioFrom),
                                new KeyValuePair<string,string>("Body", $"Pedido {orderId} confirmado. Detalhes: {payload.Substring(0, Math.Min(200, payload.Length))}")
                            };
                            var reqMsg = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url) { Content = new System.Net.Http.FormUrlEncodedContent(form) };
                            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                            reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                            await clientTw.SendAsync(reqMsg);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                    }
                }
            }
        }
        return Results.Ok();
    }
    catch (Exception)
    {
        return Results.BadRequest();
    }
});

app.Run();