using System.ComponentModel.DataAnnotations;
namespace LTW09.Models;
public class ShopOrder
{
    public int OrderId { get; set; }
    [Required, StringLength(30)] public string OrderCode { get; set; } = "";
    public int? CustomerId { get; set; }
    [Required, StringLength(160)] public string RecipientName { get; set; } = "";
    [Required, StringLength(30)] public string RecipientPhone { get; set; } = "";
    [Required, StringLength(500)] public string DeliveryAddress { get; set; } = "";
    public DateTime? DeliveryDate { get; set; }
    [StringLength(500)] public string? CardMessage { get; set; }
    [StringLength(1000)] public string? CustomerNote { get; set; }
    [Required, StringLength(30)] public string PaymentMethod { get; set; } = "COD";
    [Required, StringLength(30)] public string Status { get; set; } = "Pending";
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<ShopOrderItem> Items { get; set; } = new List<ShopOrderItem>();
}