namespace LTW09.ViewModels;

public class AdminUserViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public string PrimaryRole => Roles.Contains("Admin") ? "Admin" :
                                 Roles.Contains("Nhân viên") ? "Nhân viên" :
                                 Roles.Contains("Khách hàng") ? "Khách hàng" :
                                 (Roles.FirstOrDefault() ?? "Chưa gán");
}
