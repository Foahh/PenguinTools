using System.IO;
using PenguinTools.Chart.Parser.mgxc;
using PenguinTools.Chart.Parser.ugc;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Diagnostic;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Models;
using PenguinTools.Resources;
using PenguinTools.Services;

namespace PenguinTools.ViewModels;

using umgr = Chart.Models.umgr;

public class MusicViewModel : WatchViewModel<MusicModel>
{
    private readonly IFileDialogService _fileDialogs;
    private readonly IMusicExportService _musicExport;

    public MusicViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher,
        IFileDialogService fileDialogs,
        IMusicExportService musicExport)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
        _fileDialogs = fileDialogs;
        _musicExport = musicExport;
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
    {
        if (Model == null) return OperationResult.Success();
        var chart = Model.Mgxc;
        var meta = chart.Meta;
        var songId = meta.Id ?? throw new DiagnosticException(Strings.Error_Song_id_is_not_set);
        if (string.IsNullOrWhiteSpace(meta.FullBgmFilePath))
            throw new DiagnosticException(Strings.Error_Audio_file_not_found);
        if (string.IsNullOrWhiteSpace(meta.FullJacketFilePath))
            throw new DiagnosticException(Strings.Error_Jacket_file_not_found);
        if (meta.IsCustomStage)
        {
            if (string.IsNullOrWhiteSpace(meta.FullBgiFilePath))
                throw new DiagnosticException(Strings.Error_Background_file_is_not_set);
            if (meta.StageId is null) throw new DiagnosticException(Strings.Error_Stage_id_is_not_set);
        }

        var path = await _fileDialogs.PickFolderAsync(
            Strings.Title_Select_the_output_folder,
            Path.GetDirectoryName((string?)ModelPath));
        if (path is null) return OperationResult.Success();

        return await _musicExport.ExportAsync(Model, path, ct);
    }

    protected override async Task<OperationResult<MusicModel>> ReadModel(string path, CancellationToken ct = default)
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
            return OperationResult<MusicModel>.Failure().WithDiagnostics(parsed.Diagnostics);
        return OperationResult<MusicModel>.Success(new MusicModel(value)).WithDiagnostics(parsed.Diagnostics);
    }
}