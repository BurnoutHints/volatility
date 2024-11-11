namespace Vantage.ViewModels;

public partial class ResourceBrowserViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    private string? _searchText;
    private bool _isBusy;

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
#pragma warning restore CA1822 // Mark members as static
}
