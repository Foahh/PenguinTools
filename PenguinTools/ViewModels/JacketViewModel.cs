using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using PenguinTools.Common.Graphic;
using PenguinTools.Common.Resources;
using System.IO;
using System.Media;

namespace PenguinTools.ViewModels;

public partial class JacketViewModel : ActionViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputFileName))]
    public partial int? JacketId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputFileName))]
    [NotifyCanExecuteChangedFor(nameof(ActionCommand))]
    public partial string JacketPath { get; set; } = string.Empty;

    public string OutputFileName
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

    protected async override Task Action()
    {
        var fileName = JacketId is null ? Path.GetFileNameWithoutExtension((string?)JacketPath) : $"{(int)JacketId:0000}";
        var dlg = new SaveFileDialog
        {
            Filter = Strings.Filefilter_dds,
            FileName = $"CHU_UI_Jacket_{fileName}"
        };
        if (dlg.ShowDialog() != true) return;

        var converter = new JacketConverter();
        var options = new JacketConverter.Context(JacketPath, dlg.FileName);
        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(options, diag, p, ct));

        SystemSounds.Exclamation.Play();
    }
}