using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Vantage;

public partial class CategoryGroupControl : UserControl
{
    public CategoryGroupControl()
    {
        InitializeComponent();
    }

    public CategoryGroupControl(string categoryName)
    {
        InitializeComponent();
        DataContext = new CategoryGroupViewModel(categoryName);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}