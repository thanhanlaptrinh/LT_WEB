using System.ComponentModel.DataAnnotations;

namespace LTW09.Models;

public class Product
{
    public int ProductId { get; set; }
    [Required] public int CategoryId { get; set; }
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [Required, StringLength(180)] public string Slug { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [StringLength(80)] public string? Label { get; set; }
    [Range(0, 9999999999999999)] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int StockQuantity { get; set; }
    [Required, StringLength(500)] public string ImageUrl { get; set; } = "/assets/tulips.jpg";
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Category? Category { get; set; }
}