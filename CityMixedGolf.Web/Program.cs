using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;
using CityMixedGolf.Web.Services;
using SendGrid.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<GolfPlayer, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Only register SendGrid if an API key is configured — avoids startup crash in dev
var sendGridKey = builder.Configuration["SendGrid:ApiKey"];
if (!string.IsNullOrWhiteSpace(sendGridKey))
{
    builder.Services.AddSendGrid(options => options.ApiKey = sendGridKey);
    builder.Services.AddScoped<INotificationService, NotificationService>();
}
else
{
    // Register a no-op notification service so the app runs without SendGrid in dev
    builder.Services.AddScoped<INotificationService, NoOpNotificationService>();
}

builder.Services.AddScoped<IDrawService, DrawService>();
builder.Services.AddScoped<IPlayerImportService, PlayerImportService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    await DataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();