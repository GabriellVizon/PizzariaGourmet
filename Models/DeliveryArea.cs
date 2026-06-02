using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PizzariaGourmet.Models;

public class DeliveryArea
{
    [Key]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cepStart")]
    public string CepStart { get; set; } = string.Empty;

    [JsonPropertyName("cepEnd")]
    public string CepEnd { get; set; } = string.Empty;

    [JsonPropertyName("neighborhood")]
    public string Neighborhood { get; set; } = string.Empty;

    [JsonPropertyName("deliveryFee")]
    public decimal DeliveryFee { get; set; } = 5.00m;

    [JsonPropertyName("minOrder")]
    public decimal MinOrder { get; set; } = 0;

    [JsonPropertyName("estimatedTime")]
    public int EstimatedTime { get; set; } = 60;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}
