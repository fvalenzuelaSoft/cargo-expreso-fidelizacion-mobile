using CargoExpreso.App.Services;

namespace CargoExpreso.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        base.OnStart();

        // Navigate based on stored authentication state
        var isAuthenticated = await SecureStorageHelper.IsAuthenticatedAsync();
        await Shell.Current.GoToAsync(isAuthenticated ? "//Dashboard" : "//Login");
    }
}
