using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Chart.Parser;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Infrastructure;
using PenguinTools.Models;
using PenguinTools.Services;
using System.IO;

namespace PenguinTools.ViewModels;

public class WorkflowViewModel : WatchViewModel<WorkflowModel>
{
    private readonly IFileDialogService _fileDialogs;
    private readonly IWorkflowExportService _workflowExport;

    public WorkflowViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher,
        IFileDialogService fileDialogs,
        IWorkflowExportService workflowExport)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
        _fileDialogs = fileDialogs;
        _workflowExport = workflowExport;
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
    {
        if (Model == null) return OperationResult.Success();
        var chart = Model.Mgxc;
        var meta = chart.Meta;
        var songId = meta.Id ?? throw new DiagnosticException(Strings.Error_Song_id_is_not_set);
        if (string.IsNullOrWhiteSpace(meta.FullBgmFilePath)) throw new DiagnosticException(Strings.Error_Audio_file_not_found);
        if (string.IsNullOrWhiteSpace(meta.FullJacketFilePath)) throw new DiagnosticException(Strings.Error_Jacket_file_not_found);
        if (meta.IsCustomStage)
        {
            if (string.IsNullOrWhiteSpace(meta.FullBgiFilePath)) throw new DiagnosticException(Strings.Error_Background_file_is_not_set);
            if (meta.StageId is null) throw new DiagnosticException(Strings.Error_Stage_id_is_not_set);
        }

        var path = await _fileDialogs.PickFolderAsync(
            Strings.Title_Select_the_output_folder,
            Path.GetDirectoryName((string?)ModelPath));
        if (path is null) return OperationResult.Success();

        return await _workflowExport.ExportAsync(Model, path, ct);
    }

    protected override async Task<OperationResult<WorkflowModel>> ReadModel(string path, CancellationToken ct = default)
    {
        var parser = new MgxcParser(new MgxcParseRequest(path, AssetManager), MediaTool);
        var chart = await parser.ParseAsync(ct);
        if (!chart.Succeeded || chart.Value is not { } value) return OperationResult<WorkflowModel>.Failure().WithDiagnostics(chart.Diagnostics);
        return OperationResult<WorkflowModel>.Success(new WorkflowModel(value)).WithDiagnostics(chart.Diagnostics);
    }
}
