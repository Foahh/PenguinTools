using System.Collections.ObjectModel;
using System.Windows;
using PenguinTools.Core.Diagnostic;
using PenguinTools.Views;
using DiagnosticsWindowViewModel = PenguinTools.Views.DiagnosticsWindowViewModel;

namespace PenguinTools.Services;

public sealed class DiagnosticsPresenter : IDiagnosticsPresenter
{
    private readonly Lazy<MainWindow> _mainWindow;

    public DiagnosticsPresenter(Lazy<MainWindow> mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void Show(DiagnosticSnapshot snapshot)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var model = new DiagnosticsWindowViewModel
            {
                Diagnostics = new ObservableCollection<Diagnostic>(snapshot.Diagnostics
                    .OrderByDescending(diagnostic => diagnostic.Severity)
                    .ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Line)
                    .ThenBy(diagnostic => diagnostic.Time)
                    .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal))
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
