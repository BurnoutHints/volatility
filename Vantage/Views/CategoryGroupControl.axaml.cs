using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Vantage;

public partial class CategoryGroupControl : UserControl
{
    public CategoryGroupControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}