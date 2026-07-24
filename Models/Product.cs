using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DomPizzaria.Models;

public class Product
{
    [Key]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("available")]
    public bool Available { get; set; } = true;

    [JsonPropertyName("sizesJson")]
    public string? SizesJson { get; set; }
}
