using System.Windows;
using PenguinTools.Controls;
using PenguinTools.Core;
using PenguinTools.Views;

namespace PenguinTools.Services;

public sealed class DiagnosticsPresenter : IDiagnosticsPresenter
{
    private readonly Lazy<MainWindow> _mainWindow;

    public DiagnosticsPresenter(Lazy<MainWindow> mainWindow) => _mainWindow = mainWindow;

    public void Show(DiagnosticSnapshot snapshot)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var model = new DiagnosticsWindowViewModel
            {
                Diagnostics = [.. snapshot.Diagnostics]
            };
            var window = new DiagnosticsWindow
            {
                DataContext = model,
                Owner = _mainWindow.Value
            };
            window.ShowDialog();
        });
    }
}
