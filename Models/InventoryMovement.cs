using System.ComponentModel.DataAnnotations;
namespace LTW09.Models;
public class InventoryMovement
{
    public int InventoryMovementId { get; set; }
    public int ProductId { get; set; }
    public int QuantityChange { get; set; } // dương: nhập/hoàn; âm: bán/giảm
    [Required, StringLength(20)] public string MovementType { get; set; } = "Adjustment";
    [StringLength(60)] public string? ReferenceCode { get; set; }
    [StringLength(500)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public Product? Product { get; set; }
}