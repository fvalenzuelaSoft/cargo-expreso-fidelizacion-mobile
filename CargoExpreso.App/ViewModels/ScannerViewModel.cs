using CargoExpreso.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoExpreso.App.ViewModels;

public partial class ScannerViewModel : BaseViewModel
{
    private readonly IPointsService _points;
    private bool _isProcessing;

    public ScannerViewModel(IPointsService points) => _points = points;

    [ObservableProperty] bool   isScanning       = true;
    [ObservableProperty] string shipmentNumber   = string.Empty;
    [ObservableProperty] bool   showResult       = false;
    [ObservableProperty] string resultTitle      = string.Empty;
    [ObservableProperty] string resultDetail     = string.Empty;
    [ObservableProperty] bool   resultIsSuccess  = false;

    // Called from the ZXing scanner event
    public async Task HandleBarcodeAsync(string code)
    {
        if (_isProcessing || string.IsNullOrWhiteSpace(code)) return;
        ShipmentNumber = code;
        await ProcessAsync();
    }

    [RelayCommand]
    async Task ProcessAsync()
    {
        if (_isProcessing || string.IsNullOrWhiteSpace(ShipmentNumber)) return;

        _isProcessing = true;
        IsScanning    = false;
        ShowResult    = false;
        IsBusy        = true;
        ClearMessages();

        var result = await _points.ScanShipmentAsync(ShipmentNumber.Trim());

        IsBusy = false;

        ResultIsSuccess = result?.Success == true;

        if (result?.Success == true && result.Data is not null)
        {
            ResultTitle  = "¡Guía procesada!";
            ResultDetail = $"+{result.Data.PointsAwarded:N0} puntos acreditados\nSaldo actual: {result.Data.NewBalance:N0} puntos";
        }
        else
        {
            ResultTitle  = "No se pudo procesar";
            ResultDetail = result?.Message ?? "Error al procesar la guía. Intenta de nuevo.";
        }

        ShowResult    = true;
        _isProcessing = false;
    }

    [RelayCommand]
    void ScanAnother()
    {
        ShowResult     = false;
        IsScanning     = true;
        ShipmentNumber = string.Empty;
        ClearMessages();
    }

    [RelayCommand]
    static Task GoBack() =>
        Shell.Current.GoToAsync("..");
}
