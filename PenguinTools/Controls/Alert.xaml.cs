using PenguinTools.Core;
using System.Windows;
using System.Windows.Controls;

namespace PenguinTools.Controls;

public partial class Alert : UserControl
{
    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(nameof(Level), typeof(Severity), typeof(Alert), new PropertyMetadata(Severity.Information));
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(string), typeof(Alert), new PropertyMetadata(string.Empty));

    public Alert()
    {
        InitializeComponent();
    }

    public Severity Level
    {
        get => (Severity)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}