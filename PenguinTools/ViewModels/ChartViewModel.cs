using Microsoft.Win32;
using PenguinTools.Common;
using PenguinTools.Common.Chart;
using PenguinTools.Common.Chart.Parser;
using PenguinTools.Common.Resources;
using PenguinTools.Models;
using System.IO;

namespace PenguinTools.ViewModels;

public class ChartViewModel : WatchViewModel<ChartModel>
{
    protected async override Task Action()
    {
        if (Model == null) return;
        var chart = Model.Chart;
        var meta = chart.Meta;

        var left = Model.Id is null ? Path.GetFileNameWithoutExtension(meta.FilePath) : $"{(int)Model.Id:0000}";
        var right = $"_{(int)Model.Difficulty:00}";

        var dlg = new SaveFileDialog
        {
            Filter = Strings.Filefilter_c2s,
            FileName = left + right
        };
        if (dlg.ShowDialog() != true) return;

        var converter = new ChartConverter();
        var options = new ChartConverter.Context(dlg.FileName, chart);
        await ActionService.RunAsync(async (diag, p, ct) => await converter.ConvertAsync(options, diag, p, ct));
    }

    protected async override Task<ChartModel> ReadModel(string path, IDiagnostic d, IProgress<string> p, CancellationToken ct = default)
    {
        var parser = new MgxcParser(d, AssetManager);
        var chart = await parser.ParseAsync(path, ct);
        return new ChartModel(chart);
    }
}