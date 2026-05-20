using CargoExpreso.App.Views;

namespace CargoExpreso.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Stack navigation routes (modal / pushed pages)
        Routing.RegisterRoute("Scanner",  typeof(ScannerPage));
        Routing.RegisterRoute("Canje",    typeof(CanjePage));
        Routing.RegisterRoute("Historial", typeof(HistorialPage));
    }
}
