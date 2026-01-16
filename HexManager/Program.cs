using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using HexManager.Data;
using HexManager.Models;
using HexManager.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US");
    options.SupportedCultures = new[] { culture };
    options.SupportedUICultures = new[] { culture };
});

// Add DbContext with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=hexmanager.db"));

// Add our services
builder.Services.AddScoped<ITrafficSignalService, TrafficSignalService>();
builder.Services.AddScoped<IHexGeneratorService, HexGeneratorService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Add HttpContextAccessor for session access
builder.Services.AddHttpContextAccessor();

// Add session support for authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure database is created and seed initial admin user
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Ensuring database is created...");
        context.Database.EnsureCreated();
        logger.LogInformation("Database ensured.");
        
        // Seed initial admin user if not exists
        var adminExists = await context.Users.AnyAsync(u => u.Username == "admin");
        if (!adminExists)
        {
            var adminPassword = "(n)4zo$7^F|0<c\"6cP`)DjK20Od<}!Fm$";
            var adminEmail = builder.Configuration["Authentication:AuthorizedEmail"] ?? "admin@hexmanager.com";
            
            logger.LogInformation("Creating initial admin user with email: {Email}", adminEmail);
            
            var adminUser = new HexManager.Models.User
            {
                Username = "admin",
                Password = adminPassword,
                Email = adminEmail,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Admin user created successfully. Username: admin");
        }
        else
        {
            logger.LogInformation("Admin user already exists.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database initialization: {Error}", ex.Message);
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Use request localization
app.UseRequestLocalization();

// Use session
app.UseSession();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
