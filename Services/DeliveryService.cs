using Microsoft.EntityFrameworkCore;
using PizzariaGourmet.Data;
using PizzariaGourmet.Models;

namespace PizzariaGourmet.Services;

public class DeliveryService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DeliveryService> _logger;

    public DeliveryService(AppDbContext db, ILogger<DeliveryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Delivery Areas
    public async Task<List<DeliveryArea>> GetAllAreasAsync()
    {
        return await _db.DeliveryAreas.Where(a => a.IsActive).ToListAsync();
    }

    public async Task<DeliveryArea?> GetAreaByIdAsync(int id)
    {
        return await _db.DeliveryAreas.FindAsync(id);
    }

    public async Task<DeliveryArea> CreateAreaAsync(DeliveryArea area)
    {
        _db.DeliveryAreas.Add(area);
        await _db.SaveChangesAsync();
        return area;
    }

    public async Task<DeliveryArea?> UpdateAreaAsync(int id, DeliveryArea updated)
    {
        var area = await _db.DeliveryAreas.FindAsync(id);
        if (area == null) return null;
        area.Name = updated.Name;
        area.CepStart = updated.CepStart;
        area.CepEnd = updated.CepEnd;
        area.Neighborhood = updated.Neighborhood;
        area.DeliveryFee = updated.DeliveryFee;
        area.MinOrder = updated.MinOrder;
        area.EstimatedTime = updated.EstimatedTime;
        area.IsActive = updated.IsActive;
        await _db.SaveChangesAsync();
        return area;
    }

    public async Task<bool> DeleteAreaAsync(int id)
    {
        var area = await _db.DeliveryAreas.FindAsync(id);
        if (area == null) return false;
        _db.DeliveryAreas.Remove(area);
        await _db.SaveChangesAsync();
        return true;
    }

    // Delivery Persons
    public async Task<List<DeliveryPerson>> GetAllPersonsAsync()
    {
        return await _db.DeliveryPersons.Where(p => p.IsActive).OrderByDescending(p => p.IsAvailable).ToListAsync();
    }

    public async Task<DeliveryPerson?> GetPersonByIdAsync(int id)
    {
        return await _db.DeliveryPersons.FindAsync(id);
    }

    public async Task<DeliveryPerson> CreatePersonAsync(DeliveryPerson person)
    {
        _db.DeliveryPersons.Add(person);
        await _db.SaveChangesAsync();
        return person;
    }

    public async Task<DeliveryPerson?> UpdatePersonAsync(int id, DeliveryPerson updated)
    {
        var person = await _db.DeliveryPersons.FindAsync(id);
        if (person == null) return null;
        person.Name = updated.Name;
        person.Phone = updated.Phone;
        person.Vehicle = updated.Vehicle;
        person.IsAvailable = updated.IsAvailable;
        person.IsActive = updated.IsActive;
        await _db.SaveChangesAsync();
        return person;
    }

    public async Task<bool> DeletePersonAsync(int id)
    {
        var person = await _db.DeliveryPersons.FindAsync(id);
        if (person == null) return false;
        _db.DeliveryPersons.Remove(person);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DeliveryPerson>> GetAvailablePersonsAsync()
    {
        return await _db.DeliveryPersons.Where(p => p.IsAvailable && p.IsActive).ToListAsync();
    }

    // Business Hours
    public async Task<List<BusinessHours>> GetAllHoursAsync()
    {
        return await _db.BusinessHours.OrderBy(h => h.DayOfWeek).ToListAsync();
    }

    public async Task UpdateHoursAsync(List<BusinessHours> hours)
    {
        foreach (var h in hours)
        {
            var existing = await _db.BusinessHours.FindAsync(h.Id);
            if (existing != null)
            {
                existing.OpenTime = h.OpenTime;
                existing.CloseTime = h.CloseTime;
                existing.IsOpen = h.IsOpen;
            }
        }
        await _db.SaveChangesAsync();
    }

    public bool IsOpenNow(List<BusinessHours> hours)
    {
        var now = DateTime.Now;
        var dayOfWeek = (int)now.DayOfWeek;
        var today = hours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
        if (today == null || !today.IsOpen) return false;

        var current = now.ToString("HH:mm");
        return string.Compare(current, today.OpenTime) >= 0 && string.Compare(current, today.CloseTime) <= 0;
    }
}
