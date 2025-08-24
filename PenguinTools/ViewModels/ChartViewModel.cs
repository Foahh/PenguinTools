using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Chart.Converter;
using PenguinTools.Core.Chart.Parser;
using PenguinTools.Core.Resources;
using PenguinTools.Models;
using System.IO;

namespace PenguinTools.ViewModels;

public class ChartViewModel : WatchViewModel<ChartModel>
{
    protected override async Task Action(IDiagnostic diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        if (Model == null) return;
        var chart = Model.Mgxc;
        var meta = chart.Meta;

        var left = Model.Id is null ? Path.GetFileNameWithoutExtension(meta.FilePath) : $"{(int)Model.Id:0000}";
        var right = $"_{(int)Model.Difficulty:00}";

        var dlg = new SaveFileDialog
        {
            Filter = Strings.Filefilter_c2s,
            FileName = left + right
        };
        if (dlg.ShowDialog() != true) return;


        var converter = new C2SConverter(diag, prog)
        {
            OutPath = dlg.FileName,
            Mgxc = chart
        };
        await converter.ConvertAsync(ct);
    }

    protected override async Task<ChartModel> ReadModel(string path, IDiagnostic diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        var parser = new MgxcParser(diag, prog)
        {
            Assets = AssetManager,
            Path = path
        };
        var chart = await parser.ConvertAsync(ct);
        return new ChartModel(chart);
    }
}