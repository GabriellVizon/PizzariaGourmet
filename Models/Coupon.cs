using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PizzariaGourmet.Models;

public class Coupon
{
    [Key]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("discountType")]
    public string DiscountType { get; set; } = "percentage";

    [Required]
    [JsonPropertyName("discountValue")]
    public decimal DiscountValue { get; set; }

    [JsonPropertyName("minOrder")]
    public decimal MinOrder { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("maxUses")]
    public int MaxUses { get; set; }

    [JsonPropertyName("usedCount")]
    public int UsedCount { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}
