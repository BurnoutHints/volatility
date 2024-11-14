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
            new FileDialogFilter { Name = "Cgs Binary Resource", Extensions = { "dat", "bin" } },
            new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
        }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0)
        {
            // Show the custom enum selection dialog
            var binaryResourceTypeSelectionDialog = new BinaryResourceTypeSelectionDialog();
            var (format, resourceType) = await binaryResourceTypeSelectionDialog.ShowDialog(this);

            if (DataContext is QuickEditorViewModel vm)
            {
                // Pass the selected file, format, and resource type to the ViewModel
                vm.OnFileSelected(result[0], format, resourceType);
            }
        }
    }

}