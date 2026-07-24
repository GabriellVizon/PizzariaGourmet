using Microsoft.EntityFrameworkCore;
using DomPizzaria.Data;
using DomPizzaria.Models;

namespace DomPizzaria.Services;

public class ProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _db.Products.OrderBy(p => p.Id).ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products.FindAsync(id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        var maxId = await _db.Products.AnyAsync() ? await _db.Products.MaxAsync(p => p.Id) : 0;
        product.Id = maxId + 1;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateAsync(int id, Product updated)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return null;

        product.Name = updated.Name;
        product.Description = updated.Description;
        product.Price = updated.Price;
        product.Image = updated.Image;
        product.Category = updated.Category;
        product.Available = updated.Available;
        product.SizesJson = updated.SizesJson;

        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return false;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return true;
    }
}
