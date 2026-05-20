using CargoExpreso.App.Models;
using CargoExpreso.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoExpreso.App.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth) => _auth = auth;

    [ObservableProperty] string identityNumber = string.Empty;
    [ObservableProperty] string phone          = string.Empty;

    [RelayCommand]
    async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(IdentityNumber) || string.IsNullOrWhiteSpace(Phone))
        {
            SetError("Ingresa tu número de identidad y teléfono.");
            return;
        }

        IsBusy = true;
        ClearMessages();

        var result = await _auth.LoginAsync(new LoginRequest
        {
            IdentityNumber = IdentityNumber,
            Phone          = Phone
        });

        IsBusy = false;

        if (result?.Success != true)
        {
            SetError(result?.Message ?? "Credenciales incorrectas. Verifica tus datos.");
            return;
        }

        await Shell.Current.GoToAsync("//Dashboard");
    }

    [RelayCommand]
    static Task GoToRegister() =>
        Shell.Current.GoToAsync("//Registro");
}
