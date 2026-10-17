using System.ComponentModel.DataAnnotations;
namespace LTW09.Models;
public class ShopOrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    [Required, StringLength(160)] public string ProductName { get; set; } = "";
    [StringLength(500)] public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    [Range(1, 10000)] public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public ShopOrder? Order { get; set; }
    public Product? Product { get; set; }
}