using Microsoft.EntityFrameworkCore;
using DomPizzaria.Data;
using DomPizzaria.Models;

namespace DomPizzaria.Services;

public class ComplementService
{
    private readonly AppDbContext _db;

    public ComplementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Complement>> GetAllAsync()
    {
        return await _db.Complements.OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<List<Complement>> GetAvailableAsync()
    {
        return await _db.Complements.Where(c => c.Available).OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<Complement?> GetByIdAsync(int id)
    {
        return await _db.Complements.FindAsync(id);
    }

    public async Task<Complement> CreateAsync(Complement complement)
    {
        var maxId = await _db.Complements.AnyAsync() ? await _db.Complements.MaxAsync(c => c.Id) : 0;
        complement.Id = maxId + 1;
        _db.Complements.Add(complement);
        await _db.SaveChangesAsync();
        return complement;
    }

    public async Task<Complement?> UpdateAsync(int id, Complement updated)
    {
        var complement = await _db.Complements.FindAsync(id);
        if (complement == null) return null;

        complement.Name = updated.Name;
        complement.Price = updated.Price;
        complement.Available = updated.Available;

        await _db.SaveChangesAsync();
        return complement;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var complement = await _db.Complements.FindAsync(id);
        if (complement == null) return false;

        _db.Complements.Remove(complement);
        await _db.SaveChangesAsync();
        return true;
    }
}
