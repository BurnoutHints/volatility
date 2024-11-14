using Avalonia.Threading;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using Vantage.ViewModels;


public class CategoryGroupViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ToggleExpandCommand { get; }

    public string CategoryName { get; }
    public ObservableCollection<FieldViewModel> Fields { get; }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    public CategoryGroupViewModel(string categoryName)
    {
        CategoryName = categoryName;
        Fields = new ObservableCollection<FieldViewModel>();
        IsExpanded = true;

        ToggleExpandCommand = ReactiveCommand.Create(() =>
        {
            Dispatcher.UIThread.Post(() => IsExpanded = !IsExpanded);
        });
    }

    public override string ToString()
    {
        return $"Category: {CategoryName}, IsExpanded: {IsExpanded}";
    }
}
