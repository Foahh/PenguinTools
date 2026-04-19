using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Chart.Writer;
using PenguinTools.Chart.Parser;
using PenguinTools.Chart.Models;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Infrastructure;
using PenguinTools.Models;
using PenguinTools.Services;
using System.IO;

namespace PenguinTools.ViewModels;

using mg = PenguinTools.Chart.Models.mgxc;

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
        OperationResult<mg.Chart> parsed;
        if (string.Equals(Path.GetExtension(path), ".ugc", StringComparison.OrdinalIgnoreCase))
        {
            var r = await new UgcParser(new UgcParseRequest(path, AssetManager), MediaTool).ParseAsync(ct);
            parsed = r.Succeeded && r.Value is { } v
                ? OperationResult<mg.Chart>.Success(v).WithDiagnostics(r.Diagnostics)
                : OperationResult<mg.Chart>.Failure().WithDiagnostics(r.Diagnostics);
        }
        else
        {
            var r = await new MgxcParser(new MgxcParseRequest(path, AssetManager), MediaTool).ParseAsync(ct);
            parsed = r.Succeeded && r.Value is { } v
                ? OperationResult<mg.Chart>.Success(v).WithDiagnostics(r.Diagnostics)
                : OperationResult<mg.Chart>.Failure().WithDiagnostics(r.Diagnostics);
        }

        if (!parsed.Succeeded || parsed.Value is not { } value) return OperationResult<ChartModel>.Failure().WithDiagnostics(parsed.Diagnostics);
        return OperationResult<ChartModel>.Success(new ChartModel(value)).WithDiagnostics(parsed.Diagnostics);
    }
}
