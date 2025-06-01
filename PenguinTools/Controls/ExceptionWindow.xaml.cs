using System.Windows;

namespace PenguinTools.Controls;

public partial class ExceptionWindow : Window
{
    private static readonly DependencyProperty StackTraceProperty = DependencyProperty.Register(nameof(StackTrace), typeof(string), typeof(ExceptionWindow), new PropertyMetadata(string.Empty));

    public ExceptionWindow()
    {
        InitializeComponent();
    }

    public string StackTrace
    {
        get => (string)GetValue(StackTraceProperty);
        set => SetValue(StackTraceProperty, value);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(StackTrace);
        }
        catch
        {
            // do nothing
        }
    }
}