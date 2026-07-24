using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DomPizzaria.Models;

public class BusinessHours
{
    [Key]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("dayOfWeek")]
    public int DayOfWeek { get; set; }

    [JsonPropertyName("openTime")]
    public string OpenTime { get; set; } = "18:00";

    [JsonPropertyName("closeTime")]
    public string CloseTime { get; set; } = "23:59";

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; } = true;
}
