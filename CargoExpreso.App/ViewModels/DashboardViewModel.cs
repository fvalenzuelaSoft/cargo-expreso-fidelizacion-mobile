using CargoExpreso.App.Models;
using CargoExpreso.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CargoExpreso.App.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IPointsService _points;
    private readonly IAuthService   _auth;

    public DashboardViewModel(IPointsService points, IAuthService auth)
    {
        _points = points;
        _auth   = auth;
    }

    [ObservableProperty] string  customerName  = string.Empty;
    [ObservableProperty] decimal balance        = 0;
    [ObservableProperty] bool    canRedeem      = false;
    [ObservableProperty] bool    isRefreshing          = false;
    [ObservableProperty] bool    hasRecentTransactions = false;

    public ObservableCollection<PointsTransaction> RecentTransactions { get; } = [];

    public string BalanceDisplay => $"{Balance:N0}";

    partial void OnBalanceChanged(decimal value)
    {
        CanRedeem = value >= 200;
        OnPropertyChanged(nameof(BalanceDisplay));
    }

    public async Task LoadAsync()
    {
        IsBusy       = true;
        IsRefreshing = true;
        ClearMessages();

        CustomerName = await SecureStorageHelper.GetCustomerNameAsync() ?? "Cliente";

        var balanceResult = await _points.GetBalanceAsync();
        if (balanceResult?.Success == true && balanceResult.Data is not null)
            Balance = balanceResult.Data.Balance;

        var historyResult = await _points.GetHistoryAsync(page: 1, pageSize: 5);
        if (historyResult?.Success == true && historyResult.Data is not null)
        {
            RecentTransactions.Clear();
            foreach (var t in historyResult.Data.Transactions)
                RecentTransactions.Add(t);

            HasRecentTransactions = RecentTransactions.Count > 0;
        }

        IsBusy       = false;
        IsRefreshing = false;
    }

    [RelayCommand]
    static Task ScanGuide() =>
        Shell.Current.GoToAsync("Scanner");

    [RelayCommand]
    static Task Redeem() =>
        Shell.Current.GoToAsync("Canje");

    [RelayCommand]
    static Task ViewHistory() =>
        Shell.Current.GoToAsync("Historial");

    [RelayCommand]
    async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    async Task LogoutAsync()
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Cerrar sesión", "¿Deseas cerrar tu sesión?", "Sí", "No");

        if (!confirmed) return;

        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//Login");
    }
}
