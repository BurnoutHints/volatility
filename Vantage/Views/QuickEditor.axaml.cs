using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Interactivity;

using Vantage.ViewModels;

namespace Vantage.Views;

public partial class QuickEditor : Window
{
    public QuickEditor()
    {
        InitializeComponent();
        OpenMenuItem.Click += OpenFile;
        DataContext = new QuickEditorViewModel();
    }

    private async void OpenFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Title = "Open File",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "Cgs Resource", Extensions = { "dat" } },
                new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0)
        {
            if (DataContext is QuickEditorViewModel vm)
            {
                vm.OnFileSelected(result[0]);
            }
        }
    }
}