using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace PenguinTools.Converters;

public record EnumDescription(Enum Value, string Description);

public class EnumDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        if (value is IEnumerable enumerable) return enumerable.OfType<Enum>().Select(v => new EnumDescription(v, v.GetDescription()));
        var type = value.GetType();
        if (!type.IsEnum) return null;
        var enums = Enum.GetValues(type).Cast<Enum>();
        return enums.Select(v => new EnumDescription(v, v.GetDescription()));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();
        var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? ((DescriptionAttribute)attributes[0]).Description : value.ToString();
    }
}