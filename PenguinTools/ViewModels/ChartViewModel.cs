using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Chart.Writer;
using PenguinTools.Core.Chart.Parser;
using PenguinTools.Core.Resources;
using PenguinTools.Models;
using System.IO;

namespace PenguinTools.ViewModels;

public class ChartViewModel : WatchViewModel<ChartModel>
{
    protected override async Task<OperationResult> Action(OperationContext context, CancellationToken ct = default)
    {
        if (Model == null) return OperationResult.Success();
        var chart = Model.Mgxc;
        var meta = chart.Meta;

        var left = Model.Id is null ? Path.GetFileNameWithoutExtension(meta.FilePath) : $"{(int)Model.Id:0000}";
        var right = $"_{(int)Model.Difficulty:00}";

        var dlg = new SaveFileDialog
        {
            Filter = Strings.Filefilter_c2s,
            FileName = left + right
        };
        if (dlg.ShowDialog() != true) return OperationResult.Success();


        var writer = new C2SChartWriter(new C2SWriteRequest(dlg.FileName, chart), context);
        return await writer.WriteAsync(ct);
    }

    protected override async Task<OperationResult<ChartModel>> ReadModel(string path, OperationContext context, CancellationToken ct = default)
    {
        var parser = new MgxcParser(new MgxcParseRequest(path, AssetManager), MediaTool, context);
        var chart = await parser.ParseAsync(ct);
        if (!chart.Succeeded || chart.Value is not { } value) return OperationResult<ChartModel>.Failure().WithDiagnostics(chart.Diagnostics);
        return OperationResult<ChartModel>.Success(new ChartModel(value)).WithDiagnostics(chart.Diagnostics);
    }
}
