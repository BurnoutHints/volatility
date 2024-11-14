using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using System;
using System.Threading.Tasks;

using Volatility.Resources;

namespace Vantage;

public partial class BinaryResourceTypeSelectionDialog : Window
{
    private ComboBox _formatComboBox;
    private ComboBox _resourceTypeComboBox;
    private Button _confirmButton;

    public BinaryResourceTypeSelectionDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "Select Format and Resource Type";
        Width = 300;
        Height = 200;

        var dialogStack = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(10),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        // Combobox for format selection
        _formatComboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(Platform)), // Use ItemsSource instead of Items
            SelectedIndex = 0
        };
        dialogStack.Children.Add(new TextBlock { Text = "Select File Format:" });
        dialogStack.Children.Add(_formatComboBox);

        // Combobox for resource type selection
        _resourceTypeComboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(ResourceType)),
            SelectedIndex = 0
        };
        dialogStack.Children.Add(new TextBlock { Text = "Select Resource Type:" });
        dialogStack.Children.Add(_resourceTypeComboBox);

        // Confirm button
        _confirmButton = new Button { Content = "Confirm" };
        _confirmButton.Click += OnConfirmButtonClick;
        dialogStack.Children.Add(_confirmButton);

        Content = dialogStack;
    }

    private TaskCompletionSource<(Platform, ResourceType)> _taskCompletionSource = new TaskCompletionSource<(Platform, ResourceType)>();

    public Task<(Platform, ResourceType)> ShowDialog(Window parent)
    {
        Show(parent);
        return _taskCompletionSource.Task; // Return the task to await
    }

    private void OnConfirmButtonClick(object sender, RoutedEventArgs e)
    {
        var format = (Platform)_formatComboBox.SelectedItem;
        var resourceType = (ResourceType)_resourceTypeComboBox.SelectedItem;
        _taskCompletionSource.SetResult((format, resourceType));
        Close(); // Close without parameters
    }
}
