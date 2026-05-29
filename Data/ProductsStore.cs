using System.Text.Json;
using System.IO;
using System.Collections.Generic;

namespace PizzariaGourmet.Data
{
    public record Product(int Id, string Name, string Description, decimal Price, string Image);

    public static class ProductsStore
    {
        public static string ProductsPath() => Path.Combine(AppContext.BaseDirectory, "Data", "products.json");

        public static async Task<List<Product>> ReadProductsAsync()
        {
            var path = ProductsPath();
            if (!System.IO.File.Exists(path)) return new List<Product>();
            var json = await System.IO.File.ReadAllTextAsync(path);
            try { return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>(); } catch { return new List<Product>(); }
        }

        public static async Task WriteProductsAsync(List<Product> list)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProductsPath())!);
            var opts = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(ProductsPath(), JsonSerializer.Serialize(list, opts));
        }
    }
}
