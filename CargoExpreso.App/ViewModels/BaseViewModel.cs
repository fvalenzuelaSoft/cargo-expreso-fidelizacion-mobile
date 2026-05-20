using CommunityToolkit.Mvvm.ComponentModel;

namespace CargoExpreso.App.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    bool isBusy;

    [ObservableProperty] string errorMessage   = string.Empty;
    [ObservableProperty] string successMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;

    protected void ClearMessages()
    {
        ErrorMessage   = string.Empty;
        SuccessMessage = string.Empty;
    }

    protected void SetError(string message)
    {
        SuccessMessage = string.Empty;
        ErrorMessage   = message;
    }

    protected void SetSuccess(string message)
    {
        ErrorMessage   = string.Empty;
        SuccessMessage = message;
    }
}
