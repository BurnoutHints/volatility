using System;

using Avalonia.Controls.Templates;
using Avalonia.Controls;

namespace Vantage;

public class VantageTemplateSelector : IDataTemplate
{
    public bool Match(object data)
    {
        return true;
    }

    public Control Build(object data)
    {
        if (data is bool)
        {
            return new CheckBox { IsChecked = (bool)data };
        }
        else if (data is Enum)
        {
            var comboBox = new ComboBox();
            return comboBox;
        }
        else if (data is string)
        {
            return new TextBox { Text = data as string };
        }
        return new TextBlock { Text = data?.ToString() };
    }
}
