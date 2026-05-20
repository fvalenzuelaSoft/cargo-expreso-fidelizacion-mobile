using CargoExpreso.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoExpreso.App.ViewModels;

public partial class CanjeViewModel : BaseViewModel
{
    private readonly IRedemptionService _redemption;
    private readonly IPointsService     _points;
    private System.Timers.Timer?        _timer;

    public CanjeViewModel(IRedemptionService redemption, IPointsService points)
    {
        _redemption = redemption;
        _points     = points;
    }

    [ObservableProperty] decimal       availableBalance = 0;
    [ObservableProperty] string        amountText       = string.Empty;
    [ObservableProperty] bool          showQr           = false;
    [ObservableProperty] ImageSource?  qrImageSource;
    [ObservableProperty] string        timeRemaining    = string.Empty;
    [ObservableProperty] decimal       generatedAmount  = 0;
    [ObservableProperty] string        expiryInfo       = string.Empty;

    private DateTime _expiresAt;

    public async Task InitializeAsync()
    {
        ClearMessages();
        ShowQr     = false;
        AmountText = string.Empty;

        var result = await _points.GetBalanceAsync();
        if (result?.Success == true && result.Data is not null)
            AvailableBalance = result.Data.Balance;
    }

    [RelayCommand]
    async Task GenerateQrAsync()
    {
        if (!decimal.TryParse(AmountText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            SetError("Ingresa un monto válido.");
            return;
        }

        if (amount > AvailableBalance)
        {
            SetError($"Saldo insuficiente. Disponible: {AvailableBalance:N0} puntos.");
            return;
        }

        IsBusy = true;
        ClearMessages();

        var result = await _redemption.CreateRedemptionAsync(amount);

        IsBusy = false;

        if (result?.Success != true || result.Data is null)
        {
            SetError(result?.Message ?? "No se pudo generar el QR. Intenta de nuevo.");
            return;
        }

        var qr    = result.Data;
        var bytes = Convert.FromBase64String(qr.QrCodeBase64);
        QrImageSource   = ImageSource.FromStream(() => new MemoryStream(bytes));
        GeneratedAmount = qr.Amount;
        _expiresAt      = qr.ExpiresAt;
        AvailableBalance = qr.RemainingBalance;
        ExpiryInfo      = $"Válido hasta: {qr.ExpiresAt.ToLocalTime():HH:mm}";
        ShowQr          = true;

        StartCountdown();
    }

    private void StartCountdown()
    {
        _timer?.Stop();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (_, _) =>
        {
            var remaining = _expiresAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                _timer?.Stop();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ShowQr = false;
                    SetError("El QR expiró. Genera uno nuevo.");
                });
                return;
            }
            MainThread.BeginInvokeOnMainThread(() =>
                TimeRemaining = $"Expira en: {(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}");
        };
        _timer.Start();
    }

    [RelayCommand]
    void NewQr()
    {
        _timer?.Stop();
        ShowQr     = false;
        AmountText = string.Empty;
        ClearMessages();
    }

    [RelayCommand]
    Task GoBack()
    {
        _timer?.Stop();
        return Shell.Current.GoToAsync("..");
    }
}
