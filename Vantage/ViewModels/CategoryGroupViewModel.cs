using System.Collections.ObjectModel;
using System.Reactive;

using ReactiveUI;

namespace Vantage.ViewModels;

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

    public CategoryGroupViewModel()
    {
        Fields = [];
        IsExpanded = true;
        
        ToggleExpandCommand = ReactiveCommand.Create(() =>
        {
            IsExpanded = !IsExpanded;
        });
    }

    public CategoryGroupViewModel(string categoryName = "Default") : this()
    {
        CategoryName = categoryName;
    }

    public override string ToString()
    {
        return $"Category: {CategoryName}, IsExpanded: {IsExpanded}";
    }
}
