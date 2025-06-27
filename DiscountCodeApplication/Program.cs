using DiscountCodeApplication.Hubs;
using DiscountCodeApplication.Redis;
using DiscountCodeApplication.Repository;
using DiscountCodeApplication.Services;
using DiscountCodeApplication.Services.Interfaces;
using DiscountCodeApplication.UnitOfWork;
using Serilog;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using DiscountCodeApplication.DB;
using DiscountCodeApplication;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Redis connection
services.AddSingleton(sp => RedisConnectionFactory.Connection);
services.AddSingleton<ICacheService, RedisCacheService>();
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetSection("Redis");
    var connectionString = configuration["ConnectionString"];
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new ArgumentNullException(nameof(connectionString), "Redis connection string is missing");
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", builder =>
        builder.WithOrigins("http://localhost:5000", "https://localhost:5000") // Fix array syntax to proper method calls
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials()
               .SetIsOriginAllowed(host => true) // for SignalR CORS
        );
});

// Serilog setup: async sinks for console and file
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Async(a => a.Console())
    .WriteTo.Async(a => a.File("Logs/log-.txt", rollingInterval: RollingInterval.Day))
    .ReadFrom.Configuration(builder.Configuration) // Ensure Serilog.Settings.Configuration is referenced  
    .CreateLogger();
builder.Host.UseSerilog(Log.Logger); // Pass the logger instance explicitly  

// Repositories & Services
services.AddScoped<IDiscountCodeRepository, CachingDiscountCodeRepository>();
services.AddSingleton<IDiscountCodeGenerator, DiscountCodeGenerator>();
services.AddScoped<IDiscountCodeService, DiscountCodeService>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddHostedService<DiscountCodePreloadHostedService>();

// Database context
services.AddDbContext<DiscountCodeContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowOrigin"); // Apply the CORS policy

app.UseAuthorization();

app.MapRazorPages();

app.MapHub<DiscountCodeHub>("/discountCodeHub");

app.Run();
