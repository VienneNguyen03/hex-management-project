using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HexManager.Data;
using HexManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HexManager.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		// Database path
		string dbPath = Path.Combine(FileSystem.AppDataDirectory, "hexmanager.db");

		// Add hardcoded configuration for testing (bypassing appconfig.json build bug on macOS)
		builder.Configuration["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}";
		
		// Add services
		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddHttpClient();
		
		builder.Services.AddDbContext<ApplicationDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		// Add our shared services
		builder.Services.AddScoped<ITrafficSignalService, TrafficSignalService>();
		builder.Services.AddSingleton<IHexGeneratorService, HexGeneratorService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Initialize Database
		using (var scope = app.Services.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			context.Database.EnsureCreated();
		}

		return app;
	}
}
