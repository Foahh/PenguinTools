using System.Globalization;
using System.Windows.Data;

namespace PenguinTools.Converters;

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return !boolValue;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return !boolValue;
        return false;
    }
}