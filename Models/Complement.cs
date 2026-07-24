using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DomPizzaria.Models;

public class Complement
{
    [Key]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("available")]
    public bool Available { get; set; } = true;
}
