using DoAnWebTMDT.Data;
using DoAnWebTMDT.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ZaloPayController>();
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<WebBanHangTmdtContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("WebTMDT"));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Accounts/Login";  
        options.LogoutPath = "/Accounts/Logout";
        options.AccessDeniedPath = "/Accounts/AccessDenied"; 
    });

builder.Services.AddAuthorization();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()    // Cho phép mọi nguồn gửi request
                   .AllowAnyMethod()    // Cho phép tất cả phương thức (GET, POST, PUT, DELETE,...)
                   .AllowAnyHeader();   // Cho phép mọi header
        });
});

builder.Services.AddHttpClient(); // Không cần đăng ký HttpClient riêng cho controller
builder.Services.AddHttpClient<ZaloPayController>();

var app = builder.Build();
app.UseSession();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=CustomerIndex}/{id?}");
app.MapControllers();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/ChatHub"); // Đăng ký hub
});
app.Run();
