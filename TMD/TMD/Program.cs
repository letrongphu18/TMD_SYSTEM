using Microsoft.EntityFrameworkCore;
using TMD.Models;
using TMDSystem.Helpers;
using TMDSystem.Services;
using TMDSystem.Hubs;
using TMD.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. ADD SERVICES TO CONTAINER
// ============================================

// Controllers with Views
builder.Services.AddControllersWithViews();

// Database Context
builder.Services.AddDbContext<TmdContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================
// 2. SESSION CONFIGURATION
// ============================================
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromHours(8);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmailService, EmailService>();

// HttpClient
builder.Services.AddHttpClient();

// ============================================
// 3. DEPENDENCY INJECTION
// ============================================

// Helpers
builder.Services.AddScoped<AuditHelper>();

// Services
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddHostedService<AutoRejectRequestsService>();
builder.Services.AddScoped<ChatService>();



// SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, ClaimBasedUserIdProvider>();
// ============================================
// 4. CONFIGURATION SETTINGS
// ============================================

// Email Settings (appsettings.json -> EmailSettings section)


// ============================================
// 5. FILE UPLOAD SIZE LIMITS (10MB)
// ============================================

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = 10_485_760;
	options.ValueLengthLimit = 10_485_760;
});

builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
	options.Limits.MaxRequestBodySize = 10_485_760;
});

// ============================================
// 6. BUILD APP
// ============================================

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(policy => policy
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .SetIsOriginAllowed(origin => true));
app.UseRouting();

// ✅ IMPORTANT: Session must be before UseAuthorization
app.UseSession();
app.UseAuthorization();

// ============================================
// 7. ROUTE MAPPING
// ============================================

// SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");
// Default Controller Route
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Account}/{action=Login}/{id?}");

// ============================================
// 8. CREATE UPLOADS DIRECTORY
// ============================================

var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads", "attendance");
if (!Directory.Exists(uploadsPath))
{
	Directory.CreateDirectory(uploadsPath);
	Console.WriteLine($"✅ Created uploads directory: {uploadsPath}");
}

// ============================================
// 9. STARTUP LOGGING
// ============================================

Console.WriteLine("\n╔════════════════════════════════════════════╗");
Console.WriteLine("║     🚀 TMD SYSTEM IS STARTING...          ║");
Console.WriteLine("╚════════════════════════════════════════════╝");
Console.WriteLine($"📁 Upload folder: {uploadsPath}");
Console.WriteLine("⏰ Using SERVER TIME for attendance records");
Console.WriteLine("🌍 Reverse Geocoding: OpenStreetMap Nominatim");
Console.WriteLine("📸 Max file size: 10MB (JPG, JPEG, PNG)");
Console.WriteLine("🔔 SignalR Hub: /notificationHub");
Console.WriteLine("📧 Email Service: Gmail SMTP");
Console.WriteLine("🔐 Password Reset: OTP (3 minutes expiry)");
Console.WriteLine("══════════════════════════════════════════════\n");

// ============================================
// 10. RUN APP
// ============================================

app.Run();