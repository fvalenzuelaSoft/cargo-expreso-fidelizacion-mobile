using CargoExpreso.App.ViewModels;
using ZXing.Net.Maui;

namespace CargoExpreso.App.Views;

public partial class ScannerPage : ContentPage
{
    private readonly ScannerViewModel _vm;

    public ScannerPage(ScannerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats    = BarcodeFormats.TwoDimensional | BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple   = false
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BarcodeReader.IsDetecting = true;
        _vm.ScanAnotherCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
    }

    private async void BarcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        BarcodeReader.IsDetecting = false;
        var code = e.Results.FirstOrDefault()?.Value;
        if (!string.IsNullOrEmpty(code))
            await _vm.HandleBarcodeAsync(code);
    }
}
