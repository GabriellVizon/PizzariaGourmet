using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DomPizzaria.Data;
using DomPizzaria.Models;
using DomPizzaria.Services;

var builder = WebApplication.CreateBuilder(args);

var dbPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Data", "app.db");
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<DomPizzaria.Data.AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<DomPizzaria.Data.AppDbContext>()
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

builder.Services.AddScoped<DomPizzaria.Services.ProductService>();
builder.Services.AddScoped<DomPizzaria.Services.OrderService>();
builder.Services.AddScoped<DomPizzaria.Services.NotificationService>();
builder.Services.AddScoped<DomPizzaria.Services.ComplementService>();
builder.Services.AddScoped<DomPizzaria.Services.CouponService>();
builder.Services.AddScoped<DomPizzaria.Services.WhatsAppService>();
builder.Services.AddScoped<DomPizzaria.Services.CustomerService>();
builder.Services.AddScoped<DomPizzaria.Services.DeliveryService>();
builder.Services.AddScoped<DomPizzaria.Services.ReportService>();

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DomPizzaria.Data.AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Migration: add missing columns for existing databases
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN Discount TEXT DEFAULT 0"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN CouponCode TEXT"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Coupons (Id INTEGER PRIMARY KEY, Code TEXT NOT NULL UNIQUE, DiscountType TEXT NOT NULL DEFAULT 'percentage', DiscountValue TEXT NOT NULL DEFAULT 0, MinOrder TEXT NOT NULL DEFAULT 0, ExpiresAt TEXT, MaxUses INTEGER NOT NULL DEFAULT 0, UsedCount INTEGER NOT NULL DEFAULT 0, IsActive INTEGER NOT NULL DEFAULT 1)"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN ScheduledTime TEXT"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN DeliveryPersonId INTEGER"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN DeliveryPersonName TEXT"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN CustomerId INTEGER"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN Printed INTEGER DEFAULT 0"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN PrintedAt TEXT"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS BusinessHours (Id INTEGER PRIMARY KEY, DayOfWeek INTEGER NOT NULL, OpenTime TEXT NOT NULL DEFAULT '18:00', CloseTime TEXT NOT NULL DEFAULT '23:59', IsOpen INTEGER NOT NULL DEFAULT 1)"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS DeliveryAreas (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, CepStart TEXT DEFAULT '', CepEnd TEXT DEFAULT '', Neighborhood TEXT DEFAULT '', DeliveryFee TEXT NOT NULL DEFAULT 5.00, MinOrder TEXT NOT NULL DEFAULT 0, EstimatedTime INTEGER NOT NULL DEFAULT 60, IsActive INTEGER NOT NULL DEFAULT 1)"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS DeliveryPersons (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Phone TEXT DEFAULT '', Vehicle TEXT DEFAULT '', IsAvailable INTEGER NOT NULL DEFAULT 1, IsActive INTEGER NOT NULL DEFAULT 1, CreatedAt TEXT NOT NULL)"); } catch { }
    try { await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Customers (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Phone TEXT DEFAULT '', Email TEXT DEFAULT '', Address TEXT DEFAULT '', Cpf TEXT DEFAULT '', TotalOrders INTEGER NOT NULL DEFAULT 0, TotalSpent TEXT NOT NULL DEFAULT 0, FirstOrderAt TEXT, LastOrderAt TEXT, Notes TEXT DEFAULT '', CreatedAt TEXT NOT NULL)"); } catch { }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Cozinha", "Atendente" };
    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var adminEmail = app.Configuration["Admin:Email"] ?? "admin@dompizzaria.com";
    var adminPass = app.Configuration["Admin:Password"] ?? "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail };
        await userManager.CreateAsync(admin, adminPass);
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    if (!await db.Products.AnyAsync())
    {
        var jsonPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Data", "products.json");
        if (System.IO.File.Exists(jsonPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(jsonPath);
            var products = JsonSerializer.Deserialize<List<DomPizzaria.Models.Product>>(json);
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
            new DomPizzaria.Models.Complement { Name = "Catupiry", Price = 3.00m, Available = true },
            new DomPizzaria.Models.Complement { Name = "Cheddar", Price = 3.00m, Available = true },
            new DomPizzaria.Models.Complement { Name = "Brigadeiro", Price = 4.00m, Available = true },
            new DomPizzaria.Models.Complement { Name = "Bacon Extra", Price = 4.00m, Available = true },
            new DomPizzaria.Models.Complement { Name = "Mussarela Extra", Price = 3.00m, Available = true },
            new DomPizzaria.Models.Complement { Name = "Calabresa Extra", Price = 3.50m, Available = true },
            new DomPizzaria.Models.Complement { Name = "Pepperoni Extra", Price = 4.50m, Available = true },
            new DomPizzaria.Models.Complement { Name = "Chocolate", Price = 5.00m, Available = true }
        );
        await db.SaveChangesAsync();
    }

    if (!await db.BusinessHours.AnyAsync())
    {
        for (int d = 0; d < 7; d++)
        {
            db.BusinessHours.Add(new DomPizzaria.Models.BusinessHours
            {
                DayOfWeek = d,
                OpenTime = d == 0 ? "18:00" : "17:00",
                CloseTime = "23:59",
                IsOpen = d != 1
            });
        }
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
            pixKey = "contato@dompizzaria.com",
            storeName = "Dom Pizzaria",
            address = "Rua Exemplo, 123 - Centro",
            cnpj = "00.000.000/0001-00",
            allowScheduling = true,
            minScheduleMinutes = 60,
            estimatedTime = 60
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

app.MapGet("/api/products", async (DomPizzaria.Services.ProductService svc) =>
    Results.Json(await svc.GetAllAsync()));

app.MapGet("/api/products/{id:int}", async (int id, DomPizzaria.Services.ProductService svc) =>
{
    var p = await svc.GetByIdAsync(id);
    return p is null ? Results.NotFound() : Results.Json(p);
});

app.MapPost("/api/products", async (HttpRequest req, DomPizzaria.Services.ProductService svc) =>
{
    var product = await JsonSerializer.DeserializeAsync<DomPizzaria.Models.Product>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (product == null) return Results.BadRequest();
    var created = await svc.CreateAsync(product);
    return Results.Created($"/api/products/{created.Id}", created);
}).RequireAuthorization();

app.MapPut("/api/products/{id:int}", async (int id, HttpRequest req, DomPizzaria.Services.ProductService svc) =>
{
    var product = await JsonSerializer.DeserializeAsync<DomPizzaria.Models.Product>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (product == null) return Results.BadRequest();
    var updated = await svc.UpdateAsync(id, product);
    return updated is null ? Results.NotFound() : Results.Json(updated);
}).RequireAuthorization();

app.MapDelete("/api/products/{id:int}", async (int id, DomPizzaria.Services.ProductService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

// Complement endpoints
app.MapGet("/api/complements", async (DomPizzaria.Services.ComplementService svc) =>
    Results.Json(await svc.GetAllAsync()));

app.MapGet("/api/complements/available", async (DomPizzaria.Services.ComplementService svc) =>
    Results.Json(await svc.GetAvailableAsync()));

app.MapGet("/api/complements/{id:int}", async (int id, DomPizzaria.Services.ComplementService svc) =>
{
    var c = await svc.GetByIdAsync(id);
    return c is null ? Results.NotFound() : Results.Json(c);
});

app.MapPost("/api/complements", async (HttpRequest req, DomPizzaria.Services.ComplementService svc) =>
{
    var complement = await JsonSerializer.DeserializeAsync<Complement>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (complement == null) return Results.BadRequest();
    var created = await svc.CreateAsync(complement);
    return Results.Created($"/api/complements/{created.Id}", created);
}).RequireAuthorization();

app.MapPut("/api/complements/{id:int}", async (int id, HttpRequest req, DomPizzaria.Services.ComplementService svc) =>
{
    var complement = await JsonSerializer.DeserializeAsync<Complement>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (complement == null) return Results.BadRequest();
    var updated = await svc.UpdateAsync(id, complement);
    return updated is null ? Results.NotFound() : Results.Json(updated);
}).RequireAuthorization();

app.MapDelete("/api/complements/{id:int}", async (int id, DomPizzaria.Services.ComplementService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/create-checkout-session", async (HttpRequest req, OrderService orderSvc, NotificationService notifySvc, DomPizzaria.Services.CouponService couponSvc, WhatsAppService whatsAppSvc, DomPizzaria.Services.CustomerService customerSvc, DeliveryService deliverySvc, ILogger<Program> logger) =>
{
    var domain = Environment.GetEnvironmentVariable("DOMAIN") ?? "http://localhost:5000";

    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);

    if (!doc.RootElement.TryGetProperty("cart", out var cartEl) || cartEl.ValueKind != JsonValueKind.Array)
        return Results.BadRequest(new { error = "Cart missing" });

    // Check if store is open
    var hours = await deliverySvc.GetAllHoursAsync();
    if (!deliverySvc.IsOpenNow(hours))
    {
        var isScheduled = doc.RootElement.TryGetProperty("scheduledTime", out var st) && !string.IsNullOrEmpty(st.GetString());
        if (!isScheduled)
            return Results.BadRequest(new { error = "Loja fechada. Você pode agendar um pedido para outro horário.", storeClosed = true });
    }

    var customer = doc.RootElement.TryGetProperty("customer", out var c) ? c : default;
    var paymentMethod = doc.RootElement.TryGetProperty("paymentMethod", out var pm) ? pm.GetString() ?? "card" : "card";
    var couponCode = doc.RootElement.TryGetProperty("couponCode", out var cc) ? cc.GetString() : null;
    var scheduledTime = doc.RootElement.TryGetProperty("scheduledTime", out var sch) ? sch.GetString() : null;

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

    // Find or create customer
    var custName = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
    var custPhone = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("phone", out var ph) ? ph.GetString() ?? "" : "";
    var custEmail = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
    var custCEP = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("cep", out var cepEl) ? cepEl.GetString() ?? "" : "";
    var custAddress = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "";
    var custCPF = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("cpf", out var cpf) ? cpf.GetString() ?? "" : "";
    var custNotes = customer.ValueKind == JsonValueKind.Object && customer.TryGetProperty("notes", out var notes) ? notes.GetString() ?? "" : "";

    // Validate CEP against delivery areas if configured
    var cepDigits = new string(custCEP?.Where(char.IsDigit).ToArray() ?? []);
    if (cepDigits.Length == 8)
    {
        var areas = await deliverySvc.GetAllAreasAsync();
        if (areas.Count > 0)
        {
            var matched = areas.Any(a =>
            {
                if (string.IsNullOrEmpty(a.CepStart) || string.IsNullOrEmpty(a.CepEnd)) return false;
                var start = new string(a.CepStart.Where(char.IsDigit).ToArray());
                var end = new string(a.CepEnd.Where(char.IsDigit).ToArray());
                if (start.Length != 8 || end.Length != 8) return false;
                return string.Compare(cepDigits, start) >= 0 && string.Compare(cepDigits, end) <= 0;
            });
            if (!matched)
                return Results.BadRequest(new { error = "Infelizmente não entregamos no CEP informado." });
        }
    }

    // Append CEP to address if provided
    if (!string.IsNullOrEmpty(custCEP))
        custAddress = custAddress + (string.IsNullOrEmpty(custAddress) ? "" : " - ") + "CEP: " + custCEP;

    var customerRecord = await customerSvc.FindOrCreateAsync(custName, custPhone, custEmail, custAddress, custCPF);

    var order = new Order
    {
        CustomerName = custName,
        CustomerPhone = custPhone,
        CustomerEmail = custEmail,
        CustomerAddress = custAddress,
        CustomerCPF = custCPF,
        CustomerNotes = custNotes,
        CustomerId = customerRecord.Id,
        ScheduledTime = scheduledTime,
        Items = body,
        Status = "pending",
        PaymentMethod = paymentMethod,
        Subtotal = subtotal,
        DeliveryFee = finalDeliveryFee,
        Discount = discount,
        CouponCode = couponCode,
        Total = total
    };

    await orderSvc.CreateAsync(order);
    await customerSvc.RecordOrderAsync(customerRecord.Id, total);

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

    var estimatedTime = 60;
    try
    {
        if (System.IO.File.Exists(settingsPath))
        {
            var settingsDoc = JsonDocument.Parse(System.IO.File.ReadAllText(settingsPath));
            if (settingsDoc.RootElement.TryGetProperty("estimatedTime", out var et))
                estimatedTime = et.GetInt32();
        }
    }
    catch { }

    logger.LogInformation("Order {OrderId} created with payment method {Method}", order.Id, paymentMethod);
    return Results.Json(new { url = $"{domain}/Success?order_id={order.Id}&estimatedTime={estimatedTime}", id = order.Id, orderId = order.Id });
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
app.MapGet("/api/coupons/validate", async (string code, decimal? subtotal, DomPizzaria.Services.CouponService couponSvc) =>
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
app.MapGet("/api/coupons", async (DomPizzaria.Services.CouponService svc) =>
    Results.Json(await svc.GetAllAsync()));

app.MapGet("/api/coupons/{id:int}", async (int id, DomPizzaria.Services.CouponService svc) =>
{
    var c = await svc.GetByIdAsync(id);
    return c is null ? Results.NotFound() : Results.Json(c);
});

app.MapPost("/api/coupons", async (HttpRequest req, DomPizzaria.Services.CouponService svc) =>
{
    var coupon = await JsonSerializer.DeserializeAsync<DomPizzaria.Models.Coupon>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (coupon == null) return Results.BadRequest();
    var created = await svc.CreateAsync(coupon);
    return Results.Created($"/api/coupons/{created.Id}", created);
}).RequireAuthorization();

app.MapPut("/api/coupons/{id:int}", async (int id, HttpRequest req, DomPizzaria.Services.CouponService svc) =>
{
    var coupon = await JsonSerializer.DeserializeAsync<DomPizzaria.Models.Coupon>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (coupon == null) return Results.BadRequest();
    var updated = await svc.UpdateAsync(id, coupon);
    return updated is null ? Results.NotFound() : Results.Json(updated);
}).RequireAuthorization();

app.MapDelete("/api/coupons/{id:int}", async (int id, DomPizzaria.Services.CouponService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

// Business hours check
app.MapGet("/api/business-hours/check", async (DeliveryService deliverySvc) =>
{
    var hours = await deliverySvc.GetAllHoursAsync();
    var isOpen = deliverySvc.IsOpenNow(hours);
    var now = DateTime.Now;
    var dayOfWeek = (int)now.DayOfWeek;
    var today = hours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
    return Results.Json(new
    {
        isOpen,
        currentTime = now.ToString("HH:mm"),
        openTime = today?.OpenTime,
        closeTime = today?.CloseTime,
        dayName = now.DayOfWeek.ToString()
    });
});

// Business Hours CRUD
app.MapGet("/api/business-hours", async (DeliveryService svc) =>
    Results.Json(await svc.GetAllHoursAsync()));

app.MapPut("/api/business-hours", async (HttpRequest req, DeliveryService svc) =>
{
    var hours = await JsonSerializer.DeserializeAsync<List<BusinessHours>>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (hours == null) return Results.BadRequest();
    await svc.UpdateHoursAsync(hours);
    return Results.Ok();
}).RequireAuthorization();

// Check CEP against delivery areas
app.MapGet("/api/delivery-areas/check-cep", async (string cep, DeliveryService svc) =>
{
    var digits = new string(cep?.Where(char.IsDigit).ToArray() ?? []);
    if (digits.Length != 8)
        return Results.Json(new { valid = false });

    var areas = await svc.GetAllAreasAsync();
    var matched = areas.FirstOrDefault(a =>
    {
        if (string.IsNullOrEmpty(a.CepStart) || string.IsNullOrEmpty(a.CepEnd))
            return false;
        var start = new string(a.CepStart.Where(char.IsDigit).ToArray());
        var end = new string(a.CepEnd.Where(char.IsDigit).ToArray());
        if (start.Length != 8 || end.Length != 8) return false;
        return string.Compare(digits, start) >= 0 && string.Compare(digits, end) <= 0;
    });

    if (matched != null)
        return Results.Json(new { valid = true, areaName = matched.Name, deliveryFee = matched.DeliveryFee, estimatedTime = matched.EstimatedTime, minOrder = matched.MinOrder });
    else
        return Results.Json(new { valid = false });
});

// Delivery Persons
app.MapGet("/api/delivery-persons/available", async (DeliveryService svc) =>
    Results.Json(await svc.GetAvailablePersonsAsync()));

// Reports
app.MapGet("/api/reports", async (DateTime? dateFrom, DateTime? dateTo, ReportService svc) =>
    Results.Json(await svc.GetReportAsync(dateFrom, dateTo))).RequireAuthorization();

// Kitchen print queue
app.MapGet("/api/kitchen/orders", async (OrderService orderSvc) =>
    Results.Json(await orderSvc.GetUnprintedOrdersAsync())).RequireAuthorization();

app.MapPost("/api/kitchen/print/{orderId}", async (string orderId, OrderService orderSvc) =>
{
    await orderSvc.MarkPrintedAsync(orderId);
    return Results.Ok();
}).RequireAuthorization();

// Assign delivery person to order
app.MapPost("/api/orders/{orderId}/assign", async (string orderId, HttpRequest req, OrderService orderSvc) =>
{
    using var sr = new StreamReader(req.Body);
    var body = await sr.ReadToEndAsync();
    var doc = JsonDocument.Parse(body);
    var personId = doc.RootElement.TryGetProperty("personId", out var pid) ? pid.GetInt32() : 0;
    var personName = doc.RootElement.TryGetProperty("personName", out var pn) ? pn.GetString() ?? "" : "";
    if (personId == 0) return Results.BadRequest();
    await orderSvc.AssignDeliveryPersonAsync(orderId, personId, personName);
    return Results.Ok();
}).RequireAuthorization();

app.Run();
