using System.Collections.ObjectModel;
using System.Windows;
using PenguinTools.Core.Diagnostic;
using PenguinTools.Views;
using DiagnosticsWindowViewModel = PenguinTools.Views.DiagnosticsWindowViewModel;

namespace PenguinTools.Services;

public sealed class DiagnosticsPresenter : IDiagnosticsPresenter
{
    private readonly Lazy<MainWindow> _mainWindow;
    private DiagnosticsWindow? _window;
    private DiagnosticsWindowViewModel? _model;

    public DiagnosticsPresenter(Lazy<MainWindow> mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void Show(DiagnosticSnapshot snapshot)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var diagnostics = CreateDiagnostics(snapshot);

            if (_window is { IsVisible: true } window && _model is { } model)
            {
                model.Diagnostics = diagnostics;
                if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                window.Activate();
                return;
            }

            _model = new DiagnosticsWindowViewModel
            {
                Diagnostics = diagnostics
            };
            _window = new DiagnosticsWindow
            {
                DataContext = _model,
                Owner = _mainWindow.Value
            };
            _window.Closed += (_, _) =>
            {
                _window = null;
                _model = null;
            };
            _window.ShowDialog();
        });
    }

    private static ObservableCollection<Diagnostic> CreateDiagnostics(DiagnosticSnapshot snapshot)
    {
        return new ObservableCollection<Diagnostic>(snapshot.Diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Time)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal));
    }
}
