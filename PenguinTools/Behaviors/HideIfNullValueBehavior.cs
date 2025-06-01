using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace PenguinTools.Behaviors;

public class HideIfNullValueBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(HideIfNullValueBehavior), new PropertyMetadata(null, OnValueChanged));
    public static readonly DependencyProperty InvertProperty = DependencyProperty.Register(nameof(Invert), typeof(bool), typeof(HideIfNullValueBehavior), new PropertyMetadata(false, OnInvertChanged));

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool Invert
    {
        get => (bool)GetValue(InvertProperty);
        set => SetValue(InvertProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HideIfNullValueBehavior self) return;
        self.Check(e.NewValue, self.Invert);
    }

    private static void OnInvertChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HideIfNullValueBehavior self || e.NewValue is not bool invert) return;
        self.Check(self.Value, invert);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        Check(Value, Invert);
    }

    private void Check(object? value = null, bool invert = false)
    {
        if (AssociatedObject == null) return;
        var isVisible = invert ? value == null : value != null;
        AssociatedObject.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }
}