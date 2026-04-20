using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PenguinTools.Core.Diagnostic;
using PenguinTools.Resources;

namespace PenguinTools.Views;

public enum DiagnosticFilter
{
    All,
    Errors,
    Warnings,
    Information
}

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
    public partial string Title { get; set; } =
        $"{Strings.Title_Diagnostics} v{App.Version.ToString(3)} ({App.BuildDate.ToShortDateString()})";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyAllCommand))]
    public partial ObservableCollection<Diagnostic>? Diagnostics { get; set; }

    [ObservableProperty] public partial ICollectionView? FilteredDiagnostics { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopySelectedCommand))]
    public partial Diagnostic? SelectedDiagnostic { get; set; }

    [ObservableProperty] public partial DiagnosticFilter CurrentFilter { get; set; }

    [ObservableProperty] public partial int TotalCount { get; private set; }

    [ObservableProperty] public partial int ErrorCount { get; private set; }

    [ObservableProperty] public partial int WarningCount { get; private set; }

    [ObservableProperty] public partial int InformationCount { get; private set; }

    [ObservableProperty] public partial int VisibleCount { get; private set; }

    [ObservableProperty] public partial double LocationColumnWidth { get; private set; } = double.NaN;

    [ObservableProperty] public partial double TimeColumnWidth { get; private set; } = double.NaN;

    public string FilterSummary =>
        CurrentFilter == DiagnosticFilter.All
            ? string.Format(CultureInfo.CurrentCulture, Strings.Diagnostic_FilterSummary_All, VisibleCount)
            : string.Format(CultureInfo.CurrentCulture, Strings.Diagnostic_FilterSummary_Filtered, VisibleCount,
                TotalCount);

    partial void OnDiagnosticsChanged(ObservableCollection<Diagnostic>? value)
    {
        var diagnostics = value ?? new ObservableCollection<Diagnostic>();

        TotalCount = diagnostics.Count;
        ErrorCount = diagnostics.Count(diag => diag.Severity == Severity.Error);
        WarningCount = diagnostics.Count(diag => diag.Severity == Severity.Warning);
        InformationCount = diagnostics.Count(diag => diag.Severity == Severity.Information);

        var view = CollectionViewSource.GetDefaultView(diagnostics);
        view.Filter = FilterDiagnostic;
        FilteredDiagnostics = view;

        RefreshFilterState();
    }

    partial void OnCurrentFilterChanged(DiagnosticFilter value)
    {
        RefreshFilterState();
    }

    [RelayCommand]
    private void SetFilter(string? filter)
    {
        if (!Enum.TryParse<DiagnosticFilter>(filter, true, out var parsedFilter)) return;
        CurrentFilter = parsedFilter;
    }

    [RelayCommand(CanExecute = nameof(CanCopySelected))]
    private void CopySelected()
    {
        if (SelectedDiagnostic is null) return;

        Clipboard.SetText(FormatForClipboard(SelectedDiagnostic));
    }

    [RelayCommand(CanExecute = nameof(CanCopyAll))]
    private void CopyAll()
    {
        var text = string.Join(Environment.NewLine, GetVisibleDiagnostics().Select(FormatForClipboard));
        if (string.IsNullOrWhiteSpace(text)) return;

        Clipboard.SetText(text);
    }

    private bool CanCopySelected()
    {
        return SelectedDiagnostic is not null;
    }

    private bool CanCopyAll()
    {
        return VisibleCount > 0;
    }

    private void RefreshFilterState()
    {
        FilteredDiagnostics?.Refresh();

        var visibleDiagnostics = GetVisibleDiagnostics().ToArray();
        VisibleCount = visibleDiagnostics.Length;
        LocationColumnWidth = visibleDiagnostics.Any(diag => !string.IsNullOrWhiteSpace(diag.FormattedLocation))
            ? double.NaN
            : 0;
        TimeColumnWidth = visibleDiagnostics.Any(diag => !string.IsNullOrWhiteSpace(diag.FormattedTime))
            ? double.NaN
            : 0;

        if (SelectedDiagnostic is null || !visibleDiagnostics.Contains(SelectedDiagnostic))
            SelectedDiagnostic = visibleDiagnostics.FirstOrDefault();

        OnPropertyChanged(nameof(FilterSummary));
        CopyAllCommand.NotifyCanExecuteChanged();
    }

    private bool FilterDiagnostic(object item)
    {
        return item is Diagnostic diagnostic && MatchesFilter(diagnostic);
    }

    private bool MatchesFilter(Diagnostic diagnostic)
    {
        return CurrentFilter switch
        {
            DiagnosticFilter.Errors => diagnostic.Severity == Severity.Error,
            DiagnosticFilter.Warnings => diagnostic.Severity == Severity.Warning,
            DiagnosticFilter.Information => diagnostic.Severity == Severity.Information,
            _ => true
        };
    }

    private IEnumerable<Diagnostic> GetVisibleDiagnostics()
    {
        return FilteredDiagnostics?.Cast<Diagnostic>() ?? Enumerable.Empty<Diagnostic>();
    }

    private static string FormatForClipboard(Diagnostic diagnostic)
    {
        var details = new[]
        {
            diagnostic.FormattedLocation,
            diagnostic.FormattedTime
        }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();

        var severity = diagnostic.Severity switch
        {
            Severity.Error => Strings.Diagnostic_Severity_Error,
            Severity.Warning => Strings.Diagnostic_Severity_Warning,
            _ => Strings.Diagnostic_Severity_Information
        };

        return details.Length == 0
            ? $"{severity}: {diagnostic.Message}"
            : $"{severity}: {diagnostic.Message} ({string.Join(", ", details)})";
    }
}
