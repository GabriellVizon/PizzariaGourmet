using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Stripe.Checkout;
using PizzariaGourmet.Data;
using PizzariaGourmet.Models;
using PizzariaGourmet.Services;

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
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<PizzariaGourmet.Data.AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddScoped<PizzariaGourmet.Services.ProductService>();
builder.Services.AddScoped<PizzariaGourmet.Services.OrderService>();
builder.Services.AddScoped<PizzariaGourmet.Services.NotificationService>();
builder.Services.AddScoped<PizzariaGourmet.Services.ComplementService>();
builder.Services.AddScoped<PizzariaGourmet.Services.CouponService>();
builder.Services.AddScoped<PizzariaGourmet.Services.WhatsAppService>();

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PizzariaGourmet.Data.AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Migration: add missing columns for existing databases
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN Discount TEXT DEFAULT 0"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN CouponCode TEXT"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Coupons (Id INTEGER PRIMARY KEY, Code TEXT NOT NULL UNIQUE, DiscountType TEXT NOT NULL DEFAULT 'percentage', DiscountValue TEXT NOT NULL DEFAULT 0, MinOrder TEXT NOT NULL DEFAULT 0, ExpiresAt TEXT, MaxUses INTEGER NOT NULL DEFAULT 0, UsedCount INTEGER NOT NULL DEFAULT 0, IsActive INTEGER NOT NULL DEFAULT 1)"); } catch { }

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

    if (!await db.Complements.AnyAsync())
    {
        db.Complements.AddRange(
            new PizzariaGourmet.Models.Complement { Name = "Catupiry", Price = 3.00m, Available = true },
            new PizzariaGourmet.Models.Complement { Name = "Cheddar", Price = 3.00m, Available = true },
            new PizzariaGourmet.Models.Complement { Name = "Brigadeiro", Price = 4.00m, Available = true },
            new PizzariaGourmet.Models.Complement { Name = "Bacon Extra", Price = 4.00m, Available = true },
            new PizzariaGourmet.Models.Complement { Name = "Mussarela Extra", Price = 3.00m, Available = true },
            new PizzariaGourmet.Models.Complement { Name = "Calabresa Extra", Price = 3.50m, Available = true },
            new PizzariaGourmet.Models.Complement { Name = "Pepperoni Extra", Price = 4.50m, Available = true },
            new PizzariaGourmet.Models.Complement { Name = "Chocolate", Price = 5.00m, Available = true }
        );
        await db.SaveChangesAsync();
    }
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

    if (!context.Request.Host.Host.Contains("localhost"))
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Settings endpoint
var settingsPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Data", "settings.json");

app.MapGet("/api/settings", () =>
{
    if (!System.IO.File.Exists(settingsPath))
        return Results.Json(new
        {
            deliveryFee = 5.00,
            freeDeliveryMin = 50.00,
            whatsapp = "5524992206707",
            pixKey = "contato@pizzariagourmet.com",
            storeName = "Pizzaria Gourmet"
        });
    var json = System.IO.File.ReadAllText(settingsPath);
    return Results.Content(json, "application/json");
});

app.MapPut("/api/settings", async (HttpRequest req, ILogger<Program> logger) =>
{
    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();

    // Validate JSON
    try
    {
        var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("deliveryFee", out var _) &&
            !doc.RootElement.TryGetProperty("freeDeliveryMin", out var _))
        {
            return Results.BadRequest(new { error = "Invalid settings JSON" });
        }
    }
    catch
    {
        return Results.BadRequest(new { error = "Invalid JSON" });
    }

    await System.IO.File.WriteAllTextAsync(settingsPath, body);
    logger.LogInformation("Settings updated");
    return Results.Ok();
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapPut("/api/products/{id:int}", async (int id, HttpRequest req, PizzariaGourmet.Services.ProductService svc) =>
{
    var product = await JsonSerializer.DeserializeAsync<PizzariaGourmet.Models.Product>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (product == null) return Results.BadRequest();
    var updated = await svc.UpdateAsync(id, product);
    return updated is null ? Results.NotFound() : Results.Json(updated);
}).RequireAuthorization();

app.MapDelete("/api/products/{id:int}", async (int id, PizzariaGourmet.Services.ProductService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

// Complement endpoints
app.MapGet("/api/complements", async (PizzariaGourmet.Services.ComplementService svc) =>
    Results.Json(await svc.GetAllAsync()));

app.MapGet("/api/complements/available", async (PizzariaGourmet.Services.ComplementService svc) =>
    Results.Json(await svc.GetAvailableAsync()));

app.MapGet("/api/complements/{id:int}", async (int id, PizzariaGourmet.Services.ComplementService svc) =>
{
    var c = await svc.GetByIdAsync(id);
    return c is null ? Results.NotFound() : Results.Json(c);
});

app.MapPost("/api/complements", async (HttpRequest req, PizzariaGourmet.Services.ComplementService svc) =>
{
    var complement = await JsonSerializer.DeserializeAsync<Complement>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (complement == null) return Results.BadRequest();
    var created = await svc.CreateAsync(complement);
    return Results.Created($"/api/complements/{created.Id}", created);
}).RequireAuthorization();

app.MapPut("/api/complements/{id:int}", async (int id, HttpRequest req, PizzariaGourmet.Services.ComplementService svc) =>
{
    var complement = await JsonSerializer.DeserializeAsync<Complement>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (complement == null) return Results.BadRequest();
    var updated = await svc.UpdateAsync(id, complement);
    return updated is null ? Results.NotFound() : Results.Json(updated);
}).RequireAuthorization();

app.MapDelete("/api/complements/{id:int}", async (int id, PizzariaGourmet.Services.ComplementService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/create-checkout-session", async (HttpRequest req, OrderService orderSvc, NotificationService notifySvc, PizzariaGourmet.Services.CouponService couponSvc, WhatsAppService whatsAppSvc, ILogger<Program> logger) =>
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
    var couponCode = doc.RootElement.TryGetProperty("couponCode", out var cc) ? cc.GetString() : null;

    decimal subtotal = 0;
    decimal baseSubtotal = 0;
    foreach (var item in cartEl.EnumerateArray())
    {
        var basePrice = item.GetProperty("price").GetDecimal() * item.GetProperty("qty").GetInt32();
        var itemTotal = basePrice;
        baseSubtotal += basePrice;

        if (item.TryGetProperty("complements", out var compsEl) && compsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var comp in compsEl.EnumerateArray())
                itemTotal += comp.GetProperty("price").GetDecimal() * item.GetProperty("qty").GetInt32();
        }

        subtotal += itemTotal;
    }

    // Read delivery settings from settings.json
    var deliveryFee = 5.00m;
    var freeDeliveryMin = 50.00m;
    if (System.IO.File.Exists(settingsPath))
    {
        try
        {
            var settingsDoc = JsonDocument.Parse(System.IO.File.ReadAllText(settingsPath));
            if (settingsDoc.RootElement.TryGetProperty("deliveryFee", out var df))
                deliveryFee = df.GetDecimal();
            if (settingsDoc.RootElement.TryGetProperty("freeDeliveryMin", out var fdm))
                freeDeliveryMin = fdm.GetDecimal();
        }
        catch { }
    }
    var finalDeliveryFee = baseSubtotal >= freeDeliveryMin ? 0 : deliveryFee;
    var discount = 0m;

    // Validate and apply coupon
    if (!string.IsNullOrEmpty(couponCode))
    {
        var coupon = await couponSvc.ValidateAsync(couponCode, subtotal);
        if (coupon != null)
        {
            discount = couponSvc.ApplyDiscount(coupon, subtotal);
            await couponSvc.IncrementUsedAsync(couponCode);
            logger.LogInformation("Coupon {Code} applied - discount {Discount}", couponCode, discount);
        }
    }

    var total = Math.Max(0, subtotal + finalDeliveryFee - discount);

    var order = new Order
    {
        CustomerName = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
        CustomerPhone = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("phone", out var ph) ? ph.GetString() ?? "" : "",
        CustomerEmail = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
        CustomerAddress = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "",
        CustomerCPF = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("cpf", out var cpf) ? cpf.GetString() ?? "" : "",
        CustomerNotes = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("notes", out var notes) ? notes.GetString() ?? "" : "",
        Items = body,
        Status = "pending",
        PaymentMethod = paymentMethod == "stripe" ? "stripe" : paymentMethod,
        Subtotal = subtotal,
        DeliveryFee = finalDeliveryFee,
        Discount = discount,
        CouponCode = couponCode,
        Total = total
    };

    await orderSvc.CreateAsync(order);

    // Build a simple order summary for email
    var orderSummaryHtml = "";
    try
    {
        var cartDoc = JsonDocument.Parse(body);
        if (cartDoc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var items = new List<string>();
            foreach (var item in cartDoc.RootElement.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                var qty = item.TryGetProperty("qty", out var q) ? q.GetInt32() : 1;
                var price = item.TryGetProperty("price", out var p) ? p.GetDecimal() : 0;
                var size = item.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.Object
                    ? (s.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : "") : "";
                var comps = "";
                if (item.TryGetProperty("complements", out var compsEl) && compsEl.ValueKind == JsonValueKind.Array)
                {
                    var compNames = compsEl.EnumerateArray().Select(c =>
                        c.TryGetProperty("name", out var cn) ? cn.GetString() ?? "" : "");
                    comps = " (+" + string.Join(", ", compNames) + ")";
                }
                items.Add($"<tr><td style='padding:6px 12px;border-bottom:1px solid #eee'>{name}{(string.IsNullOrEmpty(size) ? "" : $" <span style='color:#888'>- {size}</span>")}{comps}</td><td style='padding:6px 12px;border-bottom:1px solid #eee;text-align:center'>{qty}x</td><td style='padding:6px 12px;border-bottom:1px solid #eee;text-align:right'>R$ {price + (item.TryGetProperty("complements", out var _) ? (decimal?)null : 0):F2}</td></tr>");
            }
            if (items.Count > 0)
            {
                orderSummaryHtml = "<table style='width:100%;border-collapse:collapse;font-size:0.9rem'>" +
                    "<thead><tr style='background:#f8f8f8'><th style='padding:8px 12px;text-align:left'>Item</th><th style='padding:8px 12px;text-align:center'>Qtd</th><th style='padding:8px 12px;text-align:right'>Valor</th></tr></thead><tbody>" +
                    string.Join("", items) + "</tbody></table>" +
                    $"<p style='text-align:right;font-weight:700;margin-top:12px'>Total: R$ {order.Total:F2}</p>";
            }
        }
    }
    catch { }

    // Send notification for all orders
    _ = notifySvc.SendNotificationsAsync(order.Id, body);

    // Send confirmation to customer
    if (!string.IsNullOrEmpty(order.CustomerEmail))
    {
        _ = notifySvc.SendCustomerConfirmationAsync(order.CustomerEmail, order.CustomerName, order.Id, orderSummaryHtml);
    }

    // Send WhatsApp to customer
    if (!string.IsNullOrEmpty(order.CustomerPhone))
    {
        _ = whatsAppSvc.SendStatusUpdateAsync(order.CustomerPhone, order.CustomerName, order.Id, order.Status, domain);
    }

    // For Pix or Cash, send the order directly without Stripe
    if (paymentMethod != "card")
    {
        logger.LogInformation("Order {OrderId} created with payment method {Method}", order.Id, paymentMethod);
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
        var itemName = item.GetProperty("name").GetString() ?? "Item";
        var itemPrice = item.GetProperty("price").GetDecimal();

        if (item.TryGetProperty("complements", out var compsEl) && compsEl.ValueKind == JsonValueKind.Array)
        {
            var compNames = new List<string>();
            foreach (var comp in compsEl.EnumerateArray())
            {
                compNames.Add(comp.GetProperty("name").GetString() ?? "");
                itemPrice += comp.GetProperty("price").GetDecimal();
            }
            if (compNames.Count > 0)
                itemName += " (+" + string.Join(", ", compNames) + ")";
        }

        options.LineItems.Add(new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = (long)(itemPrice * 100),
                Currency = "brl",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = itemName
                }
            },
            Quantity = item.GetProperty("qty").GetInt32()
        });
    }

    var sessionService = new SessionService();
    var session = await sessionService.CreateAsync(options);

    return Results.Json(new { url = session.Url, id = session.Id, orderId = order.Id });
});

app.MapGet("/session/{id}", async (string id, ILogger<Program> logger) =>
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
        logger.LogError(ex, "Failed to retrieve Stripe session {SessionId}", id);
        return Results.BadRequest(new { error = "Failed to retrieve session" });
    }
});

// Image upload endpoint
var uploadsDir = System.IO.Path.Combine(builder.Environment.WebRootPath, "uploads");
System.IO.Directory.CreateDirectory(uploadsDir);

app.MapPost("/api/upload", async (HttpRequest req, ILogger<Program> logger) =>
{
    if (!req.HasFormContentType)
        return Results.BadRequest(new { error = "Expected form data" });

    var form = await req.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "No file provided" });

    if (file.Length > 5 * 1024 * 1024)
        return Results.BadRequest(new { error = "File too large. Maximum size is 5MB." });

    var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    if (!allowed.Contains(ext))
        return Results.BadRequest(new { error = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp" });

    var fileName = $"{Guid.NewGuid()}{ext}";
    var filePath = System.IO.Path.Combine(uploadsDir, fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    logger.LogInformation("Uploaded file {FileName} ({Size} bytes)", fileName, file.Length);
    var url = $"/uploads/{fileName}";
    return Results.Json(new { url });
}).RequireAuthorization();

// New orders count for sound notification
app.MapGet("/api/orders/new-count", async (OrderService orderSvc, HttpRequest req) =>
{
    var sinceStr = req.Query["since"];
    if (string.IsNullOrEmpty(sinceStr) || !DateTime.TryParse(sinceStr, out var since))
        since = DateTime.UtcNow.AddHours(-1);

    var count = await orderSvc.GetNewOrderCountAsync(since);
    return Results.Json(new { count, timestamp = DateTime.UtcNow.ToString("o") });
});

// Coupon validation
app.MapGet("/api/coupons/validate", async (string code, decimal? subtotal, PizzariaGourmet.Services.CouponService couponSvc) =>
{
    var coupon = await couponSvc.ValidateAsync(code, subtotal ?? 0);
    if (coupon == null)
        return Results.Json(new { valid = false, error = "Cupom inválido ou expirado." });

    var discount = couponSvc.ApplyDiscount(coupon, subtotal ?? 0);
    return Results.Json(new
    {
        valid = true,
        code = coupon.Code,
        discountType = coupon.DiscountType,
        discountValue = coupon.DiscountValue,
        discount,
        minOrder = coupon.MinOrder
    });
});

// Coupon CRUD
app.MapGet("/api/coupons", async (PizzariaGourmet.Services.CouponService svc) =>
    Results.Json(await svc.GetAllAsync()));

app.MapGet("/api/coupons/{id:int}", async (int id, PizzariaGourmet.Services.CouponService svc) =>
{
    var c = await svc.GetByIdAsync(id);
    return c is null ? Results.NotFound() : Results.Json(c);
});

app.MapPost("/api/coupons", async (HttpRequest req, PizzariaGourmet.Services.CouponService svc) =>
{
    var coupon = await JsonSerializer.DeserializeAsync<PizzariaGourmet.Models.Coupon>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (coupon == null) return Results.BadRequest();
    var created = await svc.CreateAsync(coupon);
    return Results.Created($"/api/coupons/{created.Id}", created);
}).RequireAuthorization();

app.MapPut("/api/coupons/{id:int}", async (int id, HttpRequest req, PizzariaGourmet.Services.CouponService svc) =>
{
    var coupon = await JsonSerializer.DeserializeAsync<PizzariaGourmet.Models.Coupon>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (coupon == null) return Results.BadRequest();
    var updated = await svc.UpdateAsync(id, coupon);
    return updated is null ? Results.NotFound() : Results.Json(updated);
}).RequireAuthorization();

app.MapDelete("/api/coupons/{id:int}", async (int id, PizzariaGourmet.Services.CouponService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

// Confirm payment after Stripe redirect (fallback when webhook is not configured)
app.MapPost("/api/orders/confirm-payment", async (HttpRequest req, OrderService orderSvc, NotificationService notifySvc, ILogger<Program> logger) =>
{
    var stripeKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");
    if (string.IsNullOrEmpty(stripeKey))
        return Results.BadRequest(new { error = "STRIPE_API_KEY not configured" });
    StripeConfiguration.ApiKey = stripeKey;

    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);

    if (!doc.RootElement.TryGetProperty("session_id", out var sessionIdEl))
        return Results.BadRequest(new { error = "session_id required" });

    var sessionId = sessionIdEl.GetString();
    if (string.IsNullOrEmpty(sessionId))
        return Results.BadRequest(new { error = "session_id required" });

    try
    {
        var session = await new SessionService().GetAsync(sessionId);
        if (session.PaymentStatus != "paid" && session.PaymentStatus != "completed")
            return Results.Json(new { confirmed = false, status = session.PaymentStatus });

        var orderId = session.ClientReferenceId;
        if (string.IsNullOrEmpty(orderId))
            return Results.BadRequest(new { error = "No order linked to this session" });

        var order = await orderSvc.GetByIdAsync(orderId);
        if (order == null)
            return Results.NotFound(new { error = "Order not found" });

        if (order.Status != "paid")
        {
            await orderSvc.UpdateStatusAsync(orderId, "paid");
            _ = notifySvc.SendNotificationsAsync(orderId, order.Items);
            logger.LogInformation("Order {OrderId} confirmed via confirm-payment endpoint (session {SessionId})", orderId, sessionId);
        }

        return Results.Json(new { confirmed = true, orderId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to confirm payment for session {SessionId}", sessionId);
        return Results.BadRequest(new { error = "Failed to confirm payment" });
    }
});

app.MapPost("/webhook", async (HttpRequest req, OrderService orderSvc, NotificationService notifySvc, ILogger<Program> logger) =>
{
    var json = await new StreamReader(req.Body).ReadToEndAsync();
    var sigHeader = req.Headers["Stripe-Signature"].FirstOrDefault();
    var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

    if (string.IsNullOrEmpty(webhookSecret))
    {
        logger.LogWarning("STRIPE_WEBHOOK_SECRET not set — webhook skipped. Orders paid via card will stay 'pending' until manually updated.");
        return Results.Ok();
    }

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(json, sigHeader, webhookSecret);
        if (stripeEvent.Type == "checkout.session.completed")
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
    catch (Exception ex)
    {
        logger.LogError(ex, "Stripe webhook processing failed");
        return Results.BadRequest();
    }
});

// Startup config validation
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var stripeKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");
    var stripeWebhook = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
    var smtpHost = app.Configuration["SMTP_HOST"];
    var whatsappUrl = Environment.GetEnvironmentVariable("WHATSAPP_API_URL");

    logger.LogInformation("═══════════════════════════════════════");
    logger.LogInformation("  Pizzaria Gourmet — Verificação de Config");
    logger.LogInformation("═══════════════════════════════════════");
    logger.LogInformation("  Admin: {Email}", app.Configuration["Admin:Email"] ?? "admin@pizzariagourmet.com (padrão)");

    if (!string.IsNullOrEmpty(stripeKey))
        logger.LogInformation("  ✅ Stripe API KEY configurada");
    else
        logger.LogWarning("  ⚠️  STRIPE_API_KEY não configurada — pagamentos com cartão não funcionarão");

    if (!string.IsNullOrEmpty(stripeWebhook))
        logger.LogInformation("  ✅ Stripe Webhook Secret configurado");
    else
        logger.LogWarning("  ⚠️  STRIPE_WEBHOOK_SECRET não configurada — confirmação via /api/orders/confirm-payment será usada");

    if (!string.IsNullOrEmpty(smtpHost))
        logger.LogInformation("  ✅ SMTP configurado — e-mails serão enviados");
    else
        logger.LogWarning("  ⚠️  SMTP_HOST não configurado — e-mails de confirmação não serão enviados");

    if (!string.IsNullOrEmpty(whatsappUrl))
        logger.LogInformation("  ✅ WhatsApp API configurada");
    else
        logger.LogWarning("  ⚠️  WHATSAPP_API_URL não configurada — notificações WhatsApp desativadas");

    logger.LogInformation("═══════════════════════════════════════");
});

app.Run();
