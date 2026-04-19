using System.Windows;
using System.Windows.Controls;

namespace PenguinTools.Controls;

public partial class TabShell : UserControl
{
    public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
        nameof(MainContent),
        typeof(object),
        typeof(TabShell),
        new PropertyMetadata(null));

    public static readonly DependencyProperty FooterContentProperty = DependencyProperty.Register(
        nameof(FooterContent),
        typeof(object),
        typeof(TabShell),
        new PropertyMetadata(null));

    public TabShell()
    {
        InitializeComponent();
    }

    public object? MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }
}