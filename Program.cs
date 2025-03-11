using DatingManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add logging services
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // Ensure Console logging is enabled
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Ensures Debug logs are captured

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Database Context with retry logic for transient errors
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()) // Enable retry on transient failures
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Users}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
