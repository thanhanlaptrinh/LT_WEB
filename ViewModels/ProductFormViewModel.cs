using System.ComponentModel.DataAnnotations;

namespace LTW09.ViewModels;

public class ProductFormViewModel
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(160, ErrorMessage = "Tên không quá 160 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập slug (URL).")]
    [StringLength(180, ErrorMessage = "Slug không quá 180 ký tự.")]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    [Display(Name = "Slug (URL)")]
    public string Slug { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Mô tả không quá 1000 ký tự.")]
    [Display(Name = "Mô tả ngắn")]
    public string? Description { get; set; }

    [StringLength(80, ErrorMessage = "Nhãn không quá 80 ký tự.")]
    [Display(Name = "Nhãn (Label)")]
    public string? Label { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá.")]
    [Range(0, 99_999_999, ErrorMessage = "Giá phải từ 0 đến 99,999,999.")]
    [Display(Name = "Giá (₫)")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không âm.")]
    [Display(Name = "Tồn kho")]
    public int StockQuantity { get; set; }

    [StringLength(500, ErrorMessage = "URL ảnh không quá 500 ký tự.")]
    [Display(Name = "URL ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Đang bán")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Nổi bật")]
    public bool IsFeatured { get; set; }
}
