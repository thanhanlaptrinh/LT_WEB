using System.ComponentModel.DataAnnotations;

namespace LTW09.ViewModels;

public class CheckoutViewModel
{
    // Bước 2: Thông tin người nhận
    [Required(ErrorMessage = "Vui lòng nhập họ và tên người nhận hoa.")]
    [StringLength(160, ErrorMessage = "Tên người nhận không quá 160 ký tự.")]
    [Display(Name = "Họ và tên người nhận")]
    public string RecipientName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại người nhận phải là 10 chữ số hợp lệ (ví dụ: 0901234567).")]
    [Display(Name = "Số điện thoại nhận hoa")]
    public string RecipientPhone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [Display(Name = "Email người đặt (nhận hóa đơn)")]
    public string? Email { get; set; }

    [Display(Name = "Người gửi giấu tên (Giao ẩn danh)")]
    public bool IsAnonymousSender { get; set; } = false;

    // Bước 3: Địa chỉ giao hàng Việt hóa chuẩn 4 cấp
    [Required(ErrorMessage = "Vui lòng chọn Tỉnh / Thành phố.")]
    [Display(Name = "Tỉnh / Thành phố")]
    public string Province { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn Quận / Huyện.")]
    [Display(Name = "Quận / Huyện")]
    public string District { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn Phường / Xã.")]
    [Display(Name = "Phường / Xã")]
    public string Ward { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số nhà, tên đường hoặc tòa nhà.")]
    [StringLength(300, ErrorMessage = "Địa chỉ không quá 300 ký tự.")]
    [Display(Name = "Số nhà, tên đường")]
    public string StreetAddress { get; set; } = "";

    // Địa chỉ tổng hợp lưu vào Database
    [Display(Name = "Địa chỉ giao hàng hoàn chỉnh")]
    public string DeliveryAddress { get; set; } = "";

    // Bước 4: Thời gian giao, lời nhắn, ghi chú
    [DataType(DataType.Date)]
    [Display(Name = "Ngày giao hoa mong muốn")]
    public DateTime? DeliveryDate { get; set; }

    [Display(Name = "Khung giờ giao hoa")]
    public string? DeliveryTimeSlot { get; set; } = "Giao trong ngày (08:00 - 18:00)";

    [Display(Name = "Mẫu thiệp chúc mừng có sẵn")]
    public string? CardTemplate { get; set; }

    [StringLength(500, ErrorMessage = "Nội dung thiệp không quá 500 ký tự.")]
    [Display(Name = "Lời nhắn viết trên thiệp tặng kèm")]
    public string? CardMessage { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không quá 1000 ký tự.")]
    [Display(Name = "Ghi chú thêm cho thợ hoa & shipper")]
    public string? CustomerNote { get; set; }

    // Bước 5: Phương thức thanh toán
    [Required]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD"; // COD hoặc BankTransfer

    // Danh sách sản phẩm trong giỏ
    public List<CheckoutItemViewModel> Items { get; set; } = new();

    public string GetFullAddress()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(StreetAddress)) parts.Add(StreetAddress.Trim());
        if (!string.IsNullOrWhiteSpace(Ward)) parts.Add(Ward.Trim());
        if (!string.IsNullOrWhiteSpace(District)) parts.Add(District.Trim());
        if (!string.IsNullOrWhiteSpace(Province)) parts.Add(Province.Trim());
        return string.Join(", ", parts);
    }
}

public class CheckoutItemViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã sản phẩm không hợp lệ.")]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng đặt từ 1 đến 100.")]
    public int Quantity { get; set; }
}
