using DiscountCodeService.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

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
