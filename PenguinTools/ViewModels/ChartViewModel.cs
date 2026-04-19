using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Chart.Writer;
using PenguinTools.Chart.Parser;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Infrastructure;
using PenguinTools.Models;
using PenguinTools.Services;
using System.IO;

namespace PenguinTools.ViewModels;

public class ChartViewModel : WatchViewModel<ChartModel>
{
    public ChartViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
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


        var writer = new C2SChartWriter(new C2SWriteRequest(dlg.FileName, chart));
        return await writer.WriteAsync(ct);
    }

    protected override async Task<OperationResult<ChartModel>> ReadModel(string path, CancellationToken ct = default)
    {
        var parser = new MgxcParser(new MgxcParseRequest(path, AssetManager), MediaTool);
        var chart = await parser.ParseAsync(ct);
        if (!chart.Succeeded || chart.Value is not { } value) return OperationResult<ChartModel>.Failure().WithDiagnostics(chart.Diagnostics);
        return OperationResult<ChartModel>.Success(new ChartModel(value)).WithDiagnostics(chart.Diagnostics);
    }
}
