using Microsoft.EntityFrameworkCore;
using DomPizzaria.Data;
using DomPizzaria.Models;

namespace DomPizzaria.Services;

public class CouponService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CouponService> _logger;

    public CouponService(AppDbContext db, ILogger<CouponService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Coupon>> GetAllAsync()
    {
        return await _db.Coupons.OrderBy(c => c.Code).ToListAsync();
    }

    public async Task<Coupon?> GetByIdAsync(int id)
    {
        return await _db.Coupons.FindAsync(id);
    }

    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        return await _db.Coupons.FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower().Trim());
    }

    public async Task<Coupon?> ValidateAsync(string code, decimal subtotal)
    {
        var coupon = await GetByCodeAsync(code);
        if (coupon == null)
        {
            _logger.LogWarning("Coupon {Code} not found", code);
            return null;
        }

        if (!coupon.IsActive)
        {
            _logger.LogWarning("Coupon {Code} is inactive", code);
            return null;
        }

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Coupon {Code} expired", code);
            return null;
        }

        if (coupon.MaxUses > 0 && coupon.UsedCount >= coupon.MaxUses)
        {
            _logger.LogWarning("Coupon {Code} max uses reached", code);
            return null;
        }

        if (subtotal < coupon.MinOrder)
        {
            _logger.LogWarning("Coupon {Code} min order not met ({Subtotal} < {Min})", code, subtotal, coupon.MinOrder);
            return null;
        }

        return coupon;
    }

    public decimal ApplyDiscount(Coupon coupon, decimal subtotal)
    {
        return coupon.DiscountType == "percentage"
            ? Math.Round(subtotal * coupon.DiscountValue / 100, 2)
            : Math.Min(coupon.DiscountValue, subtotal);
    }

    public async Task<Coupon> CreateAsync(Coupon coupon)
    {
        var maxId = await _db.Coupons.AnyAsync() ? await _db.Coupons.MaxAsync(c => c.Id) : 0;
        coupon.Id = maxId + 1;
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Coupon {Code} created", coupon.Code);
        return coupon;
    }

    public async Task<Coupon?> UpdateAsync(int id, Coupon updated)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon == null) return null;

        coupon.Code = updated.Code;
        coupon.DiscountType = updated.DiscountType;
        coupon.DiscountValue = updated.DiscountValue;
        coupon.MinOrder = updated.MinOrder;
        coupon.ExpiresAt = updated.ExpiresAt;
        coupon.MaxUses = updated.MaxUses;
        coupon.UsedCount = updated.UsedCount;
        coupon.IsActive = updated.IsActive;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Coupon {Code} updated", coupon.Code);
        return coupon;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon == null) return false;

        _db.Coupons.Remove(coupon);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Coupon {Code} deleted", coupon.Code);
        return true;
    }

    public async Task<bool> IncrementUsedAsync(string code)
    {
        var coupon = await GetByCodeAsync(code);
        if (coupon == null) return false;
        coupon.UsedCount++;
        await _db.SaveChangesAsync();
        return true;
    }
}
