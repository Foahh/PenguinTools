using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Chart.Writer;
using PenguinTools.Core.Chart.Parser;
using PenguinTools.Core.Media;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Models;
using PenguinTools.Services;
using System.IO;

namespace PenguinTools.ViewModels;

public class WorkflowViewModel : WatchViewModel<WorkflowModel>
{
    public WorkflowViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IEmbeddedResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider)
    {
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
    {
        if (Model == null) return OperationResult.Success();
        var diagnostics = DiagnosticSnapshot.Empty;
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

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)ModelPath),
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return OperationResult.Success();
        var path = dlg.FolderName;

        var stage = meta.Stage;
        if (meta.IsCustomStage)
        {
            var stageConverter = new StageConverter(
                new StageBuildRequest(
                    AssetManager,
                    meta.FullBgiFilePath,
                    [],
                    meta.StageId,
                    path,
                    meta.NotesFieldLine,
                    AssetProvider.GetPath(InfrastructureAsset.StageTemplate),
                    AssetProvider.GetPath(InfrastructureAsset.NotesFieldTemplate)),
                MediaTool);
            var builtStage = await stageConverter.BuildAsync(ct);
            diagnostics = diagnostics.Merge(builtStage.Diagnostics);
            if (!builtStage.Succeeded || builtStage.Value is not { } stageEntry) return OperationResult.Failure().WithDiagnostics(diagnostics);
            stage = stageEntry;
        }

        ct.ThrowIfCancellationRequested();
        var metaMap = new Dictionary<Difficulty, Meta>
        {
            [meta.Difficulty] = meta
        };
        var xml = new MusicXml(metaMap, meta.Difficulty)
        {
            StageName = stage
        };

        if (meta is { Difficulty: Difficulty.WorldsEnd or Difficulty.Ultima, UnlockEventId: { } eventId })
        {
            var type = meta.Difficulty == Difficulty.WorldsEnd ? EventXml.MusicType.WldEnd : EventXml.MusicType.Ultima;
            var eXml = new EventXml(eventId, type, [new Entry(songId, meta.Title)]);
            await eXml.SaveDirectoryAsync(path);
        }

        var musicFolder = await xml.SaveDirectoryAsync(path);
        var chartPath = Path.Combine(musicFolder, xml[meta.Difficulty].File);

        var chartWriter = new C2SChartWriter(new C2SWriteRequest(chartPath, chart));
        var writtenChart = await chartWriter.WriteAsync(ct);
        diagnostics = diagnostics.Merge(writtenChart.Diagnostics);
        if (!writtenChart.Succeeded) return OperationResult.Failure().WithDiagnostics(diagnostics);

        ct.ThrowIfCancellationRequested();

        var jacketConverter = new JacketConverter(
            new JacketConvertRequest(meta.FullJacketFilePath, Path.Combine(musicFolder, xml.JaketFile)),
            MediaTool);
        var convertedJacket = await jacketConverter.ConvertAsync(ct);
        diagnostics = diagnostics.Merge(convertedJacket.Diagnostics);
        if (!convertedJacket.Succeeded) return OperationResult.Failure().WithDiagnostics(diagnostics);

        ct.ThrowIfCancellationRequested();

        var musicConverter = new MusicConverter(
            new MusicConvertRequest(
                Model.Meta,
                path,
                AssetProvider.GetPath(InfrastructureAsset.DummyAcb),
                ResourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(Model.Meta.FullBgmFilePath)}.wav")),
            MediaTool);
        var convertedMusic = await musicConverter.ConvertAsync(ct);
        diagnostics = diagnostics.Merge(convertedMusic.Diagnostics);
        return (convertedMusic.Succeeded ? OperationResult.Success() : OperationResult.Failure()).WithDiagnostics(diagnostics);
    }

    protected override async Task<OperationResult<WorkflowModel>> ReadModel(string path, CancellationToken ct = default)
    {
        var parser = new MgxcParser(new MgxcParseRequest(path, AssetManager), MediaTool);
        var chart = await parser.ParseAsync(ct);
        if (!chart.Succeeded || chart.Value is not { } value) return OperationResult<WorkflowModel>.Failure().WithDiagnostics(chart.Diagnostics);
        return OperationResult<WorkflowModel>.Success(new WorkflowModel(value)).WithDiagnostics(chart.Diagnostics);
    }
}
