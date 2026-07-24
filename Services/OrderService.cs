using Microsoft.EntityFrameworkCore;
using DomPizzaria.Data;
using DomPizzaria.Models;

namespace DomPizzaria.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _db.Orders.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<List<Order>> SearchAsync(string? name, string? phone, string? status, DateTime? dateFrom, DateTime? dateTo)
    {
        var query = _db.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(o => o.CustomerName.Contains(name));

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digits))
                query = query.Where(o => o.CustomerPhone.Contains(digits));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

        // Date filtering in memory
        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;
            orders = orders.Where(o => DateTime.TryParse(o.CreatedAt, out var dt) && dt >= from).ToList();
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.Date.AddDays(1);
            orders = orders.Where(o => DateTime.TryParse(o.CreatedAt, out var dt) && dt < to).ToList();
        }

        return orders;
    }

    public async Task<int> GetNewOrderCountAsync(DateTime since)
    {
        var sinceStr = since.ToString("o");
        return await _db.Orders.CountAsync(o => string.Compare(o.CreatedAt, sinceStr) > 0);
    }

    public async Task<Order?> GetByIdAsync(string id)
    {
        return await _db.Orders.FindAsync(id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} created - Total: {Total}", order.Id, order.Total);
        return order;
    }

    public async Task<Order?> UpdateStatusAsync(string id, string status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for status update", id);
            return null;
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} status updated to {Status}", id, status);
        return order;
    }

    public async Task<List<Order>> GetByPhoneAsync(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return new List<Order>();

        // Search by partial phone match using EF.Functions.Like
        return await _db.Orders
            .Where(o => o.CustomerPhone.Contains(digits))
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return false;

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} deleted", id);
        return true;
    }

    public async Task<List<Order>> GetByCustomerIdAsync(int customerId)
    {
        return await _db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task AssignDeliveryPersonAsync(string orderId, int personId, string personName)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return;
        order.DeliveryPersonId = personId;
        order.DeliveryPersonName = personName;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} assigned to delivery person {PersonName}", orderId, personName);
    }

    public async Task<List<Order>> GetUnprintedOrdersAsync()
    {
        return await _db.Orders
            .Where(o => !o.Printed && o.Status != "cancelled")
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkPrintedAsync(string orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return;
        order.Printed = true;
        order.PrintedAt = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync();
    }
}
