using System.ComponentModel.DataAnnotations;
namespace LTW09.Models;
public class Customer
{
    public int CustomerId { get; set; }
    [Required, StringLength(160)] public string FullName { get; set; } = "";
    [Required, StringLength(30)] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(254)] public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ShopOrder> Orders { get; set; } = new List<ShopOrder>();
}