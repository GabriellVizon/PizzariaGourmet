using Microsoft.EntityFrameworkCore;
using PizzariaGourmet.Data;
using PizzariaGourmet.Models;

namespace PizzariaGourmet.Services;

public class CustomerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(AppDbContext db, ILogger<CustomerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _db.Customers.OrderByDescending(c => c.LastOrderAt).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _db.Customers.FindAsync(id);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return null;
        return await _db.Customers.FirstOrDefaultAsync(c => c.Phone.Contains(digits));
    }

    public async Task<Customer> FindOrCreateAsync(string name, string phone, string email, string address, string cpf)
    {
        var existing = await GetByPhoneAsync(phone);
        if (existing != null)
        {
            existing.Name = name;
            existing.Email = email;
            existing.Address = address;
            if (!string.IsNullOrEmpty(cpf)) existing.Cpf = cpf;
            await _db.SaveChangesAsync();
            return existing;
        }

        var customer = new Customer
        {
            Name = name,
            Phone = phone,
            Email = email,
            Address = address,
            Cpf = cpf,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        _logger.LogInformation("New customer created: {Name} {Phone}", name, phone);
        return customer;
    }

    public async Task RecordOrderAsync(int customerId, decimal total)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer == null) return;
        customer.TotalOrders++;
        customer.TotalSpent += total;
        customer.LastOrderAt = DateTime.UtcNow.ToString("o");
        if (customer.FirstOrderAt == null)
            customer.FirstOrderAt = customer.LastOrderAt;
        await _db.SaveChangesAsync();
    }

    public async Task<List<Customer>> SearchAsync(string? name, string? phone)
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digits))
                query = query.Where(c => c.Phone.Contains(digits));
        }
        return await query.OrderByDescending(c => c.LastOrderAt).Take(50).ToListAsync();
    }

    public async Task UpdateNotesAsync(int id, string notes)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c != null)
        {
            c.Notes = notes;
            await _db.SaveChangesAsync();
        }
    }
}
