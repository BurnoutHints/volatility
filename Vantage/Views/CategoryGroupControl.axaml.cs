using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Vantage;

public partial class CategoryGroupControl : UserControl
{
    public CategoryGroupControl()
    {
        InitializeComponent();
        this.DataContextChanged += (_, __) =>
        {
            Console.WriteLine($"(Vantage) DEBUG: CategoryGroupControl DataContext set to: {this.DataContext}");
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}