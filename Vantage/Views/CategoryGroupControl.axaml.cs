using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Vantage.ViewModels;

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


    private void ToggleExpandButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CategoryGroupViewModel vm) return;
        vm.ToggleExpandCommand.Execute();
        UpdateUIState(vm.IsExpanded);
    }

    public void UpdateUIState(bool expanded)
    {
        
    }
}