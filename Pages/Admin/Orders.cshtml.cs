using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;

public class OrdersModel : PageModel
{
    public record OrderDto(string Id, string CreatedAt, string Status);
    public List<OrderDto> Orders { get; set; } = new();

    public async Task OnGetAsync()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "app.db");
        if (!System.IO.File.Exists(dbPath)) return;
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CreatedAt, Status FROM Orders ORDER BY CreatedAt DESC;";
        using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            Orders.Add(new OrderDto(rdr.GetString(0), rdr.GetString(1), rdr.GetString(2)));
        }
    }
}
