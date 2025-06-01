using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PenguinTools.Common.Asset;
using PenguinTools.Common.Graphic;
using PenguinTools.Common.Resources;
using System.IO;
using System.Media;

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

    protected async override Task Action()
    {
        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)BackgroundPath),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return;

        string?[] effectPaths = [EffectPath0, EffectPath1, EffectPath2, EffectPath3];
        var options = new StageConverter.Context(BackgroundPath, effectPaths, StageId, dlg.FolderName, NoteFieldsLine);
        var converter = new StageConverter(AssetManager);
        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(options, diag, p, ct));
        SystemSounds.Exclamation.Play();
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