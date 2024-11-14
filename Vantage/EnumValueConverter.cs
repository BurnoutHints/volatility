using System;
using System.Linq;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Vantage;

public class EnumValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && value.GetType().IsEnum)
        {
            return Enum.GetValues(value.GetType()).Cast<object>().ToList();
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}
