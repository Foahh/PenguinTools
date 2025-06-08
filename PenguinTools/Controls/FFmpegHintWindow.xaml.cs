using PenguinTools.Common.Resources;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PenguinTools.Controls;

public partial class FFmpegHintWindow : Window
{
    public FFmpegHintWindow()
    {
        Title = string.Format(Strings.Window_Title, App.Name, App.Version.ToString(3));
        InitializeComponent();
    }

    private void OpenDocumentation_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/PenguinHot/PenguinTools/wiki") { UseShellExecute = true });
    }

    public static void ShowDialog(Window owner)
    {
        var window = new FFmpegHintWindow
        {
            Owner = owner,
        };
        window.ShowDialog();
    }
}