using Microsoft.EntityFrameworkCore;
using PizzariaGourmet.Data;
using PizzariaGourmet.Models;

namespace PizzariaGourmet.Services;

public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _db.Orders.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(string id)
    {
        return await _db.Orders.FindAsync(id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> UpdateStatusAsync(string id, string status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return null;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return false;

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return true;
    }
}
