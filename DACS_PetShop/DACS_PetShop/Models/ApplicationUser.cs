using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    // Thêm thuộc tính tùy chỉnh nếu cần
    public string FullName { get; set; }
}
