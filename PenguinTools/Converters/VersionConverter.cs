using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PenguinTools.Converters;

public class VersionConverter : IValueConverter
{
    public int FieldCount { set; get; } = 3;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Version version) return version.ToString(FieldCount);
        return value ?? DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string versionString || string.IsNullOrWhiteSpace(versionString)) return DependencyProperty.UnsetValue;
        return Version.TryParse(versionString, out var result) ? result : DependencyProperty.UnsetValue;
    }
}