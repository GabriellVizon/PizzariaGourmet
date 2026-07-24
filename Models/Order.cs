using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DomPizzaria.Models;

public class Order
{
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("customerAddress")]
    public string CustomerAddress { get; set; } = string.Empty;

    [JsonPropertyName("customerCPF")]
    public string CustomerCPF { get; set; } = string.Empty;

    [JsonPropertyName("customerNotes")]
    public string CustomerNotes { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public string Items { get; set; } = "[]";

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [JsonPropertyName("paymentId")]
    public string PaymentId { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("couponCode")]
    public string? CouponCode { get; set; }

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("scheduledTime")]
    public string? ScheduledTime { get; set; }

    [JsonPropertyName("deliveryPersonId")]
    public int? DeliveryPersonId { get; set; }

    [JsonPropertyName("deliveryPersonName")]
    public string? DeliveryPersonName { get; set; }

    [JsonPropertyName("customerId")]
    public int? CustomerId { get; set; }

    [JsonPropertyName("printed")]
    public bool Printed { get; set; } = false;

    [JsonPropertyName("printedAt")]
    public string? PrintedAt { get; set; }
}
