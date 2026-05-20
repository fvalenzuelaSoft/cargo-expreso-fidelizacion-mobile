using CargoExpreso.App.ViewModels;

namespace CargoExpreso.App.Views;

public partial class CanjePage : ContentPage
{
    private readonly CanjeViewModel _vm;

    public CanjePage(CanjeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}
