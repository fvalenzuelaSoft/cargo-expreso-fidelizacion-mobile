using CargoExpreso.App.Models;
using CargoExpreso.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CargoExpreso.App.ViewModels;

public partial class HistorialViewModel : BaseViewModel
{
    private readonly IPointsService _points;
    private int  _currentPage = 1;
    private bool _hasMore     = true;

    public HistorialViewModel(IPointsService points) => _points = points;

    [ObservableProperty] decimal currentBalance = 0;
    [ObservableProperty] bool    isRefreshing   = false;
    [ObservableProperty] bool    hasTransactions = false;

    public ObservableCollection<PointsTransaction> Transactions { get; } = [];

    public async Task LoadAsync(bool reset = true)
    {
        if (IsBusy) return;

        if (reset)
        {
            _currentPage = 1;
            _hasMore     = true;
            Transactions.Clear();
        }

        if (!_hasMore) return;

        IsBusy       = true;
        IsRefreshing = reset;
        ClearMessages();

        var result = await _points.GetHistoryAsync(page: _currentPage, pageSize: 20);

        if (result?.Success == true && result.Data is not null)
        {
            CurrentBalance = result.Data.CurrentBalance;

            foreach (var t in result.Data.Transactions)
                Transactions.Add(t);

            _hasMore = Transactions.Count < result.Data.TotalCount;
            _currentPage++;
        }
        else
        {
            SetError("No se pudo cargar el historial.");
        }

        HasTransactions = Transactions.Count > 0;
        IsBusy          = false;
        IsRefreshing     = false;
    }

    [RelayCommand]
    async Task RefreshAsync() => await LoadAsync(reset: true);

    [RelayCommand]
    async Task LoadMoreAsync()
    {
        if (_hasMore && !IsBusy)
            await LoadAsync(reset: false);
    }

    [RelayCommand]
    static Task GoBack() =>
        Shell.Current.GoToAsync("..");
}
