using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<FileStorageService>();
var databaseProvider = builder.Configuration["Database:Provider"];
var databaseConnection = builder.Configuration.GetConnectionString("VetClinic");
builder.Services.AddDbContext<VetClinicDbContext>(options =>
{
    // SQLite стартира веднага; SQL Server може да се избере през настройка.
    if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(databaseConnection);
    }
    else
    {
        options.UseSqlite(databaseConnection);
    }
});

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<VetClinicDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<VetClinicDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DatabaseSchemaUpdater.ApplyAsync(db);
    await SeedData.InitializeAsync(services, app.Environment.IsDevelopment(), app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Файловете се показват само през защитен контролер, не директно от wwwroot.

app.Run();
