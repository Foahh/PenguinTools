using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Common;
using PenguinTools.Common.Resources;
using Swordfish.NET.Collections;

namespace PenguinTools.Controls;

public partial class DiagnosticsWindow
{
    public DiagnosticsWindow()
    {
        InitializeComponent();
    }
}

public partial class DiagnosticsWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = Strings.Title_Diagnostics;

    [ObservableProperty]
    public partial ConcurrentObservableSortedSet<Diagnostic>? Diagnostics { get; set; }

    [ObservableProperty]
    public partial string? StackTrace { get; set; } = null;

    [ObservableProperty]
    public partial Diagnostic? SelectedDiagnostic { get; set; } = null;

    [ObservableProperty]
    public partial double PathColumnWidth { get; private set; } = double.NaN;

    [ObservableProperty]
    public partial double TimeColumnWidth { get; private set; } = double.NaN;

    partial void OnDiagnosticsChanged(ConcurrentObservableSortedSet<Diagnostic>? value)
    {
        if (value is null) return;
        var showPath = value.Any(diag => !string.IsNullOrWhiteSpace(diag.Path));
        PathColumnWidth = showPath ? double.NaN : 0;
        var showTime = value.Any(diag => diag.FormattedTime != null);
        TimeColumnWidth = showTime ? double.NaN : 0;
    }
}