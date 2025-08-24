using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Media;
using PenguinTools.Core.Resources;
using System.IO;

namespace PenguinTools.ViewModels;

public partial class JacketViewModel : ActionViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DestinationFileName))]
    public partial int? JacketId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DestinationFileName))]
    [NotifyCanExecuteChangedFor(nameof(ActionCommand))]
    public partial string JacketPath { get; set; } = string.Empty;

    public string DestinationFileName
    {
        get
        {
            if (JacketId is { } id) return $"[CHU_UI_Jacket_{id:0000}.dds]";
            return !string.IsNullOrWhiteSpace(JacketPath) ? $"[CHU_UI_Jacket_{Path.GetFileNameWithoutExtension((string?)JacketPath)}.dds]" : string.Empty;
        }
    }

    protected override bool CanRun()
    {
        return !string.IsNullOrWhiteSpace(JacketPath);
    }

    protected override async Task Action(Diagnoster diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        var fileName = JacketId is null ? Path.GetFileNameWithoutExtension((string?)JacketPath) : $"{(int)JacketId:0000}";
        var dlg = new SaveFileDialog
        {
            Filter = Strings.Filefilter_dds,
            FileName = $"CHU_UI_Jacket_{fileName}"
        };
        if (dlg.ShowDialog() != true) return;

        var converter = new JacketConverter(diag, prog)
        {
            InPath = JacketPath,
            OutPath = dlg.FileName,
        };
        await converter.ConvertAsync(ct);
    }
}