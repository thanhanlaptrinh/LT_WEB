using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LTW09.Models;

public class ProductReview
{
    public int ProductReviewId { get; set; }
    public int ProductId { get; set; }
    [Required] public string UserId { get; set; } = "";
    [Required, StringLength(80)] public string DisplayName { get; set; } = "";
    [Range(1, 5)] public int Stars { get; set; }
    [Required, StringLength(2000)] public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Product? Product { get; set; }
    public IdentityUser? User { get; set; }
}
