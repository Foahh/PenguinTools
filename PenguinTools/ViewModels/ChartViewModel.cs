using System.IO;
using Microsoft.Win32;
using PenguinTools.Chart.Parser.mgxc;
using PenguinTools.Chart.Parser.ugc;
using PenguinTools.Chart.Writer;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Models;
using PenguinTools.Resources;
using PenguinTools.Services;

namespace PenguinTools.ViewModels;

using umgr = Chart.Models.umgr;

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
        OperationResult<umgr.Chart> parsed;
        if (string.Equals(Path.GetExtension(path), ".ugc", StringComparison.OrdinalIgnoreCase))
        {
            var r = await new UgcParser(new UgcParseRequest(path, AssetManager), MediaTool).ParseAsync(ct);
            parsed = r is { Succeeded: true, Value: { } v }
                ? OperationResult<umgr.Chart>.Success(v).WithDiagnostics(r.Diagnostics)
                : OperationResult<umgr.Chart>.Failure().WithDiagnostics(r.Diagnostics);
        }
        else
        {
            var r = await new MgxcParser(new MgxcParseRequest(path, AssetManager), MediaTool).ParseAsync(ct);
            parsed = r is { Succeeded: true, Value: { } v }
                ? OperationResult<umgr.Chart>.Success(v).WithDiagnostics(r.Diagnostics)
                : OperationResult<umgr.Chart>.Failure().WithDiagnostics(r.Diagnostics);
        }

        if (!parsed.Succeeded || parsed.Value is not { } value)
            return OperationResult<ChartModel>.Failure().WithDiagnostics(parsed.Diagnostics);
        return OperationResult<ChartModel>.Success(new ChartModel(value)).WithDiagnostics(parsed.Diagnostics);
    }
}