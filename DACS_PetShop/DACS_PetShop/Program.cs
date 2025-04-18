using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using DACS_PetShop.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext và kết nối đến cơ sở dữ liệu
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DACS")));

// Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Tắt xác minh email
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Đăng ký IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Thêm dịch vụ session
builder.Services.AddDistributedMemoryCache(); // Lưu trữ session trong bộ nhớ
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout session
    options.Cookie.HttpOnly = true; // Bảo mật cookie
    options.Cookie.IsEssential = true; // Tuân thủ GDPR
});

// Cấu hình đường dẫn cookie (login/logout)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Google Authentication (commented-out for now, uncomment to use)
//builder.Services.AddAuthentication()
//    .AddGoogle(options =>
//    {
//        IConfigurationSection googleAuthNSection =
//            builder.Configuration.GetSection("Authentication:Google");
//        options.ClientId = googleAuthNSection["ClientId"];
//        options.ClientSecret = googleAuthNSection["ClientSecret"];
//        options.CallbackPath = "/signin-google";
//    });

// Thêm Razor Pages và MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Bắt buộc cho Identity UI
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

// Thêm middleware session
app.UseSession();

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