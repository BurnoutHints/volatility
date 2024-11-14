using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Vantage.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenQuickEditor_Click(object sender, RoutedEventArgs e)
    {
        var quickEditorWindow = new QuickEditor();
        quickEditorWindow.Show();
    }
}