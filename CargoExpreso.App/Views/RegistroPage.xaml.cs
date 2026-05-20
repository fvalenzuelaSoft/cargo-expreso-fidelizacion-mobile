using CargoExpreso.App.ViewModels;

namespace CargoExpreso.App.Views;

public partial class RegistroPage : ContentPage
{
    public RegistroPage(RegistroViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
