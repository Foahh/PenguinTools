using PenguinTools.Core.Metadata;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PenguinTools.Converters;

public class DifficultyBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Difficulty diff) return Brushes.Black;
        var level = (parameter as string)?.ToLowerInvariant();
        switch (diff)
        {
            case Difficulty.Basic:
                if (level == "normal") return new SolidColorBrush(Color.FromRgb(0x5C, 0xA9, 0x4B));
                if (level == "dark") return new SolidColorBrush(Color.FromRgb(0x40, 0x75, 0x34));
                return new SolidColorBrush(Color.FromRgb(0x32, 0x5C, 0x29));
            case Difficulty.Advanced:
                if (level == "normal") return new SolidColorBrush(Color.FromRgb(0xE0, 0x9B, 0x38));
                if (level == "dark") return new SolidColorBrush(Color.FromRgb(0xAD, 0x77, 0x2B));
                return new SolidColorBrush(Color.FromRgb(0x93, 0x66, 0x24));
            case Difficulty.Expert:
                if (level == "normal") return new SolidColorBrush(Color.FromRgb(0xCA, 0x32, 0x52));
                if (level == "dark") return new SolidColorBrush(Color.FromRgb(0x96, 0x25, 0x3D));
                return new SolidColorBrush(Color.FromRgb(0x7D, 0x1F, 0x32));
            case Difficulty.Master:
                if (level == "normal") return new SolidColorBrush(Color.FromRgb(0x9C, 0x33, 0xCA));
                if (level == "dark") return new SolidColorBrush(Color.FromRgb(0x74, 0x26, 0x96));
                return new SolidColorBrush(Color.FromRgb(0x60, 0x1F, 0x7D));
            case Difficulty.Ultima:
                if (level == "normal") return new SolidColorBrush(Color.FromRgb(0xE1, 0x3D, 0x45));
                if (level == "dark") return new SolidColorBrush(Color.FromRgb(0xAE, 0x2F, 0x35));
                return new SolidColorBrush(Color.FromRgb(0x94, 0x28, 0x2D));
            case Difficulty.WorldsEnd:
                GradientStopCollection stops = level switch
                {
                    "normal" =>
                    [
                        new GradientStop(Color.FromRgb(204, 0, 0), 0.00),
                        new GradientStop(Color.FromRgb(204, 102, 0), 0.25),
                        new GradientStop(Color.FromRgb(204, 204, 0), 0.50),
                        new GradientStop(Color.FromRgb(0, 153, 51), 0.75),
                        new GradientStop(Color.FromRgb(0, 0, 204), 1.00)
                    ],
                    "dark" =>
                    [
                        new GradientStop(Color.FromRgb(163, 0, 0), 0.00),
                        new GradientStop(Color.FromRgb(163, 82, 0), 0.25),
                        new GradientStop(Color.FromRgb(163, 163, 0), 0.50),
                        new GradientStop(Color.FromRgb(0, 122, 41), 0.75),
                        new GradientStop(Color.FromRgb(0, 0, 163), 1.00)
                    ],
                    _ =>
                    [
                        new GradientStop(Color.FromRgb(122, 0, 0), 0.00),
                        new GradientStop(Color.FromRgb(122, 61, 0), 0.25),
                        new GradientStop(Color.FromRgb(122, 122, 0), 0.50),
                        new GradientStop(Color.FromRgb(0, 91, 30), 0.75),
                        new GradientStop(Color.FromRgb(0, 0, 122), 1.00)
                    ]
                };
                return new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 0));
            default:
                return Brushes.Black;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}