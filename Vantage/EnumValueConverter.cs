using System;
using System.Linq;
using System.Globalization;
using System.Reflection;

using Avalonia.Data.Converters;

namespace Vantage;

public class EnumValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && value.GetType().IsEnum)
        {
            var enumType = value.GetType();
            return Enum.GetValues(enumType).Cast<object>().Select(enumValue =>
            {
                var fieldInfo = enumType.GetField(enumValue.ToString());
                var attribute = fieldInfo?.GetCustomAttribute<EditorLabelAttribute>();

                return new
                {
                    Value = enumValue,
                    Label = attribute?.Label ?? enumValue.ToString()
                };
            }).ToList();
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && value.GetType().GetProperty("Value") != null)
        {
            return value.GetType().GetProperty("Value").GetValue(value);
        }
        return null;
    }
}
