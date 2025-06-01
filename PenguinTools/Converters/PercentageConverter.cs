using System.Globalization;
using System.Windows.Data;

namespace PenguinTools.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double height && parameter is string param && double.TryParse(param, out var percentage))
        {
            return height * percentage;
        }
        return double.NaN;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}