using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Stripe;
using Stripe.Checkout;

var builder = WebApplication.CreateBuilder(args);

var dbPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Data", "app.db");
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<PizzariaGourmet.Data.AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<PizzariaGourmet.Data.AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Login";
});

builder.Services.AddScoped<PizzariaGourmet.Services.ProductService>();
builder.Services.AddScoped<PizzariaGourmet.Services.OrderService>();
builder.Services.AddScoped<PizzariaGourmet.Services.NotificationService>();

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PizzariaGourmet.Data.AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var adminEmail = app.Configuration["Admin:Email"] ?? "admin@pizzariagourmet.com";
    var adminPass = app.Configuration["Admin:Password"] ?? "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail };
        await userManager.CreateAsync(admin, adminPass);
    }

    if (!await db.Products.AnyAsync())
    {
        var jsonPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Data", "products.json");
        if (System.IO.File.Exists(jsonPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(jsonPath);
            var products = JsonSerializer.Deserialize<List<PizzariaGourmet.Models.Product>>(json);
            if (products != null)
            {
                db.Products.AddRange(products);
                await db.SaveChangesAsync();
            }
        }
    }
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/api/products", async (PizzariaGourmet.Services.ProductService svc) =>
    Results.Json(await svc.GetAllAsync()));

app.MapGet("/api/products/{id:int}", async (int id, PizzariaGourmet.Services.ProductService svc) =>
{
    var p = await svc.GetByIdAsync(id);
    return p is null ? Results.NotFound() : Results.Json(p);
});

app.MapPost("/api/products", async (HttpRequest req, PizzariaGourmet.Services.ProductService svc) =>
{
    var product = await JsonSerializer.DeserializeAsync<PizzariaGourmet.Models.Product>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (product == null) return Results.BadRequest();
    var created = await svc.CreateAsync(product);
    return Results.Created($"/api/products/{created.Id}", created);
});

app.MapPut("/api/products/{id:int}", async (int id, HttpRequest req, PizzariaGourmet.Services.ProductService svc) =>
{
    var product = await JsonSerializer.DeserializeAsync<PizzariaGourmet.Models.Product>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (product == null) return Results.BadRequest();
    var updated = await svc.UpdateAsync(id, product);
    return updated is null ? Results.NotFound() : Results.Json(updated);
});

app.MapDelete("/api/products/{id:int}", async (int id, PizzariaGourmet.Services.ProductService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.Ok() : Results.NotFound();
});

app.MapPost("/create-checkout-session", async (HttpRequest req, PizzariaGourmet.Services.OrderService orderSvc) =>
{
    var stripeKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");
    var domain = Environment.GetEnvironmentVariable("DOMAIN") ?? "http://localhost:5000";

    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);

    if (!doc.RootElement.TryGetProperty("cart", out var cartEl) || cartEl.ValueKind != JsonValueKind.Array)
        return Results.BadRequest(new { error = "Cart missing" });

    var customer = doc.RootElement.TryGetProperty("customer", out var c) ? c : default;
    var paymentMethod = doc.RootElement.TryGetProperty("paymentMethod", out var pm) ? pm.GetString() ?? "card" : "card";

    decimal subtotal = 0;
    foreach (var item in cartEl.EnumerateArray())
        subtotal += item.GetProperty("price").GetDecimal() * item.GetProperty("qty").GetInt32();

    var deliveryFee = subtotal >= 50 ? 0 : 5.00m;

    var order = new PizzariaGourmet.Models.Order
    {
        CustomerName = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
        CustomerPhone = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("phone", out var ph) ? ph.GetString() ?? "" : "",
        CustomerAddress = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "",
        CustomerCPF = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("cpf", out var cpf) ? cpf.GetString() ?? "" : "",
        CustomerNotes = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("notes", out var notes) ? notes.GetString() ?? "" : "",
        Items = body,
        Status = "pending",
        PaymentMethod = paymentMethod == "stripe" ? "stripe" : paymentMethod,
        Subtotal = subtotal,
        DeliveryFee = deliveryFee,
        Total = subtotal + deliveryFee
    };

    await orderSvc.CreateAsync(order);

    // For Pix or Cash, send the order directly without Stripe
    if (paymentMethod != "card")
    {
        return Results.Json(new { url = domain + "/Success?order_id=" + order.Id, id = order.Id, orderId = order.Id });
    }

    // Stripe Checkout for card payments
    if (string.IsNullOrEmpty(stripeKey))
        return Results.BadRequest(new { error = "STRIPE_API_KEY not configured" });

    StripeConfiguration.ApiKey = stripeKey;

    var options = new SessionCreateOptions
    {
        PaymentMethodTypes = new List<string> { "card" },
        Mode = "payment",
        SuccessUrl = domain + "/Success?session_id={CHECKOUT_SESSION_ID}",
        CancelUrl = domain + "/Checkout",
        LineItems = new List<SessionLineItemOptions>(),
        ClientReferenceId = order.Id
    };

    foreach (var item in cartEl.EnumerateArray())
    {
        options.LineItems.Add(new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = (long)(item.GetProperty("price").GetDecimal() * 100),
                Currency = "brl",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.GetProperty("name").GetString()
                }
            },
            Quantity = item.GetProperty("qty").GetInt32()
        });
    }

    var sessionService = new SessionService();
    var session = await sessionService.CreateAsync(options);

    return Results.Json(new { url = session.Url, id = session.Id, orderId = order.Id });
});

app.MapGet("/session/{id}", async (string id) =>
{
    var stripeKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");
    if (string.IsNullOrEmpty(stripeKey))
        return Results.BadRequest(new { error = "STRIPE_API_KEY not configured" });
    StripeConfiguration.ApiKey = stripeKey;
    try
    {
        var session = await new SessionService().GetAsync(id);
        return Results.Json(session);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/webhook", async (HttpRequest req, PizzariaGourmet.Services.OrderService orderSvc, PizzariaGourmet.Services.NotificationService notifySvc) =>
{
    var json = await new StreamReader(req.Body).ReadToEndAsync();
    var sigHeader = req.Headers["Stripe-Signature"].FirstOrDefault();
    var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

    if (string.IsNullOrEmpty(webhookSecret))
        return Results.Ok();

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(json, sigHeader, webhookSecret);
        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            var orderId = session?.ClientReferenceId;
            if (!string.IsNullOrEmpty(orderId))
            {
                await orderSvc.UpdateStatusAsync(orderId, "paid");
                var order = await orderSvc.GetByIdAsync(orderId);
                if (order != null)
                    await notifySvc.SendNotificationsAsync(orderId, order.Items);
            }
        }
        return Results.Ok();
    }
    catch
    {
        return Results.BadRequest();
    }
});

app.Run();
