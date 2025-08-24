using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Media;
using PenguinTools.Core.Resources;
using System.IO;

namespace PenguinTools.ViewModels;

public partial class StageViewModel : ActionViewModel
{
    public StageViewModel()
    {
        NoteFieldsLine = AssetManager.FieldLines.FirstOrDefault(p => p.Str == "Orange") ?? Entry.Default;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ActionCommand))]
    public partial string BackgroundPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? EffectPath0 { get; set; }

    [ObservableProperty]
    public partial string? EffectPath1 { get; set; }

    [ObservableProperty]
    public partial string? EffectPath2 { get; set; }

    [ObservableProperty]
    public partial string? EffectPath3 { get; set; }

    [ObservableProperty]
    public partial Entry NoteFieldsLine { get; set; }

    [ObservableProperty]
    public partial int StageId { get; set; }

    protected override bool CanRun()
    {
        return !string.IsNullOrWhiteSpace(BackgroundPath);
    }

    protected override async Task Action(IDiagnostic diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)BackgroundPath),
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return;

        var converter = new StageConverter(diag, prog)
        {
            Assets = AssetManager,
            BackgroundPath = BackgroundPath,
            EffectPaths = [EffectPath0, EffectPath1, EffectPath2, EffectPath3],
            StageId = StageId,
            OutFolder = dlg.FolderName,
            NoteFieldLane = NoteFieldsLine
        };

        await converter.ConvertAsync(ct);
    }

    [RelayCommand]
    private void ClearAll()
    {
        BackgroundPath = string.Empty;
        EffectPath0 = string.Empty;
        EffectPath1 = string.Empty;
        EffectPath2 = string.Empty;
        EffectPath3 = string.Empty;
    }
}