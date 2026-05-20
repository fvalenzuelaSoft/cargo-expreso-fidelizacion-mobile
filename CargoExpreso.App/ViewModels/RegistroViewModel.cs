using CargoExpreso.App.Models;
using CargoExpreso.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoExpreso.App.ViewModels;

public partial class RegistroViewModel : BaseViewModel
{
    private readonly IAuthService  _auth;

    public RegistroViewModel(IAuthService auth) => _auth = auth;

    [ObservableProperty] string identityNumber = string.Empty;
    [ObservableProperty] string firstName      = string.Empty;
    [ObservableProperty] string lastName       = string.Empty;
    [ObservableProperty] string phone          = string.Empty;
    [ObservableProperty] string email          = string.Empty;

    // Bonus preview updates when email changes
    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(BonusPreview));

    public string BonusPreview => string.IsNullOrWhiteSpace(Email)
        ? "Completa tu perfil con correo y gana puntos adicionales"
        : "Perfil con correo: recibirás puntos de bienvenida adicionales";

    [RelayCommand]
    async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(IdentityNumber) || string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName)       || string.IsNullOrWhiteSpace(Phone))
        {
            SetError("Identidad, nombre, apellido y teléfono son obligatorios.");
            return;
        }

        IsBusy = true;
        ClearMessages();

        var result = await _auth.RegisterAsync(new RegisterCustomerRequest
        {
            IdentityNumber = IdentityNumber,
            FirstName      = FirstName,
            LastName       = LastName,
            Phone          = Phone,
            Email          = string.IsNullOrWhiteSpace(Email) ? null : Email
        });

        IsBusy = false;

        if (result?.Success != true)
        {
            SetError(result?.Message ?? "No se pudo registrar. Verifica que el número de identidad no esté ya registrado.");
            return;
        }

        // Auto-login: attempt login with the same credentials
        var loginResult = await _auth.LoginAsync(new LoginRequest
        {
            IdentityNumber = IdentityNumber,
            Phone          = Phone
        });

        if (loginResult?.Success == true)
            await Shell.Current.GoToAsync("//Dashboard");
        else
            await Shell.Current.GoToAsync("//Login");
    }

    [RelayCommand]
    static Task GoToLogin() =>
        Shell.Current.GoToAsync("//Login");
}
