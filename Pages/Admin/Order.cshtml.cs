using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

public class OrderModel : PageModel
{
    [BindProperty(SupportsGet=true)] public string? Id { get; set; }
    public string CreatedAt { get; set; } = "";
    public string Status { get; set; } = "";
    public string Payload { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Admin/Orders");
        var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "app.db");
        if (!System.IO.File.Exists(dbPath)) return NotFound();
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CreatedAt, Status, Payload FROM Orders WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", Id);
        using var rdr = await cmd.ExecuteReaderAsync();
        if (await rdr.ReadAsync())
        {
            CreatedAt = rdr.GetString(0);
            Status = rdr.GetString(1);
            Payload = rdr.GetString(2);
            return Page();
        }
        return NotFound();
    }

    public async Task<IActionResult> OnPostAsync([FromForm] string status, [FromQuery] int? delete)
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Admin/Orders");
        var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "app.db");
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();

        if (delete == 1)
        {
            var del = conn.CreateCommand();
            del.CommandText = "DELETE FROM Orders WHERE Id = $id;";
            del.Parameters.AddWithValue("$id", Id);
            await del.ExecuteNonQueryAsync();
            return RedirectToPage("/Admin/Orders");
        }

        var update = conn.CreateCommand();
        update.CommandText = "UPDATE Orders SET Status = $status WHERE Id = $id;";
        update.Parameters.AddWithValue("$status", status);
        update.Parameters.AddWithValue("$id", Id);
        await update.ExecuteNonQueryAsync();

        // If status moved to paid, optionally send notifications (email/SMS)
        if (status == "paid")
        {
            var select = conn.CreateCommand();
            select.CommandText = "SELECT Payload FROM Orders WHERE Id = $id;";
            select.Parameters.AddWithValue("$id", Id);
            var payload = (string?)await select.ExecuteScalarAsync();
            if (!string.IsNullOrEmpty(payload))
            {
                // reuse helper in Program.cs? for simplicity perform basic HTTP POST to Twilio or send email via SMTP here
                // We'll call a lightweight internal endpoint to trigger notifications if available
                try { await TriggerNotificationsAsync(Id, payload); } catch { }
            }
        }

        return RedirectToPage(new { id = Id });
    }

    private async Task TriggerNotificationsAsync(string orderId, string payload)
    {
        // POST to /notify-order to reuse logic centralized in Program.cs if desired.
        var notifyUrl = "/notify-order";
        using var client = new System.Net.Http.HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var content = new StringContent(JsonSerializer.Serialize(new { id = orderId, payload }), System.Text.Encoding.UTF8, "application/json");
        await client.PostAsync(notifyUrl, content);
    }
}
