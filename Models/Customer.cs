using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PizzariaGourmet.Models;

public class Customer
{
    [Key]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("cpf")]
    public string Cpf { get; set; } = string.Empty;

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("totalSpent")]
    public decimal TotalSpent { get; set; }

    [JsonPropertyName("firstOrderAt")]
    public string? FirstOrderAt { get; set; }

    [JsonPropertyName("lastOrderAt")]
    public string? LastOrderAt { get; set; }

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
