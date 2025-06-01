using Microsoft.Xaml.Behaviors;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PenguinTools.Behaviors;

public partial class OptionNameTextBoxBehavior : Behavior<TextBox>
{
    private static readonly Regex AllowedCharactersRegex = MyRegex();

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewTextInput += OnPreviewTextInput;
        AssociatedObject.TextChanged += OnTextChanged;
        DataObject.AddPastingHandler(AssociatedObject, OnPaste);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
        AssociatedObject.TextChanged -= OnTextChanged;
        DataObject.RemovePastingHandler(AssociatedObject, OnPaste);
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (AllowedCharactersRegex.IsMatch(e.Text)) return;
        e.Handled = true;
        SystemSounds.Beep.Play();
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var caretIndex = textBox.CaretIndex;
        textBox.Text = textBox.Text.ToUpper();
        textBox.CaretIndex = caretIndex;
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var data = e.DataObject.GetData(typeof(string));
            if (data is not string pastedText) return;
            if (!AllowedCharactersRegex.IsMatch(pastedText)) e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    [GeneratedRegex("^[A-Za-z0-9]+$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}