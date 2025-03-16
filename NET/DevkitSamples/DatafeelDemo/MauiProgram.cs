using CommunityToolkit.Maui;
using Serilog;
using Serilog.Events;

namespace DatafeelDemo
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            SetupSerilog();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Helvetica.ttf", "Helvetica");
                    fonts.AddFont("fa_solid.ttf", "FontAwesome");
                });

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddScoped<MainViewModel>();
            builder.Logging.AddSerilog(dispose: true);
            builder.Services.AddSingleton<IDotService, DotService>();
            var app = builder.Build();

            //we must initialize our service helper before using it
            ServiceHelper.Initialize(app.Services);

            return app;
        }

        private static void SetupSerilog()
        {
            var flushInterval = new TimeSpan(0, 0, 1);
            var file = Path.Combine(FileSystem.AppDataDirectory, "MyApp.log");

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .CreateLogger();
        }
    }
}