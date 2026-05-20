using CargoExpreso.App.Services;
using CargoExpreso.App.ViewModels;
using CargoExpreso.App.Views;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;

namespace CargoExpreso.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",   "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf",  "OpenSansSemibold");
            });

        // ── Services ────────────────────────────────────────────────────────
        builder.Services.AddSingleton<IApiClient,        ApiClient>();
        builder.Services.AddSingleton<IAuthService,      AuthService>();
        builder.Services.AddSingleton<IPointsService,    PointsService>();
        builder.Services.AddSingleton<IRedemptionService, RedemptionService>();

        // ── ViewModels ───────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegistroViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScannerViewModel>();
        builder.Services.AddTransient<CanjeViewModel>();
        builder.Services.AddTransient<HistorialViewModel>();

        // ── Pages ────────────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistroPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<CanjePage>();
        builder.Services.AddTransient<HistorialPage>();

        return builder.Build();
    }
}
