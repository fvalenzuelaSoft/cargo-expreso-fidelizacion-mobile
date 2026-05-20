using CargoExpreso.App.ViewModels;

namespace CargoExpreso.App.Views;

public partial class HistorialPage : ContentPage
{
    private readonly HistorialViewModel _vm;

    public HistorialPage(HistorialViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
