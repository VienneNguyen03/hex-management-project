using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HexManager.Data;
using HexManager.Services;
using HexManager.Maui.Services;
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
		builder.Services.AddSingleton<IAuthService, MauiAuthService>();

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

			// One-time cleanup: normalize multiple spaces in street names from legacy CSV import
			var conn = context.Database.GetDbConnection();
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"
				UPDATE TrafficSignals SET
					StreetName1 = TRIM(REPLACE(REPLACE(REPLACE(StreetName1,'   ',' '),'  ',' '),'  ',' ')),
					StreetName2 = TRIM(REPLACE(REPLACE(REPLACE(StreetName2,'   ',' '),'  ',' '),'  ',' ')),
					StreetName3 = TRIM(REPLACE(REPLACE(REPLACE(StreetName3,'   ',' '),'  ',' '),'  ',' ')),
					StreetName4 = TRIM(REPLACE(REPLACE(REPLACE(StreetName4,'   ',' '),'  ',' '),'  ',' '))
				WHERE StreetName1 LIKE '%  %' OR StreetName2 LIKE '%  %'
				   OR StreetName3 LIKE '%  %' OR StreetName4 LIKE '%  %';";
			cmd.ExecuteNonQuery();
			conn.Close();
		}

		return app;
	}
}
