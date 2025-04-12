using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext và kết nối đến cơ sở dữ liệu
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DACS")));

// Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // ✅ Tắt xác minh email
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// (Tuỳ chọn) Nếu muốn xác minh email, cần thêm IEmailSender
// builder.Services.AddTransient<IEmailSender, YourEmailSenderClass>();

// Cấu hình đường dẫn cookie (login/logout)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
// Đăng Nhập Google



// Thêm Razor Pages và MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // 💡 BẮT BUỘC để dùng giao diện Identity mặc định
builder.Services.AddTransient<IEmailSender, EmailSender>();


var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Xác thực & phân quyền
app.UseAuthentication();
app.UseAuthorization();

// Route MVC Controller
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Route Razor Pages (bao gồm Identity UI)
app.MapRazorPages();

app.Run();
