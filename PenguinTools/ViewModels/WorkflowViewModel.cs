using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Chart.Converter;
using PenguinTools.Core.Chart.Parser;
using PenguinTools.Core.Media;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;
using PenguinTools.Models;
using System.IO;

namespace PenguinTools.ViewModels;

public class WorkflowViewModel : WatchViewModel<WorkflowModel>
{
    protected async override Task Action(IDiagnostic diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        if (Model == null) return;
        var chart = Model.Mgxc;
        var meta = chart.Meta;
        var songId = meta.Id ?? throw new DiagnosticException(Strings.Error_Song_id_is_not_set);
        if (string.IsNullOrWhiteSpace(meta.FullBgmFilePath)) throw new DiagnosticException(Strings.Error_Audio_file_is_not_set);
        if (string.IsNullOrWhiteSpace(meta.FullJacketFilePath)) throw new DiagnosticException(Strings.Error_Jacket_file_is_not_set);
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
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FolderName;

        var stage = meta.Stage;
        if (meta.IsCustomStage)
        {
            var stageConverter = new StageConverter(diag, prog)
            {
                Assets = AssetManager,
                BackgroundPath = meta.FullBgiFilePath,
                EffectPaths = [],
                StageId = meta.StageId,
                OutFolder = path,
                NoteFieldLane = meta.NotesFieldLine
            };
            stage = await stageConverter.ConvertAsync(ct);
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

        var chartConverter = new ChartConverter(diag, prog)
        {
            OutPath = chartPath,
            Mgxc = chart
        };
        await chartConverter.ConvertAsync(ct);

        ct.ThrowIfCancellationRequested();

        var jacketConverter = new JacketConverter(diag, prog)
        {
            InPath = meta.FullJacketFilePath,
            OutPath = Path.Combine(musicFolder, xml.JaketFile)
        };
        await jacketConverter.ConvertAsync(ct);

        ct.ThrowIfCancellationRequested();

        var musicConverter = new MusicConverter(diag, prog)
        {
            Meta = Model.Meta,
            OutFolder = path
        };
        await musicConverter.ConvertAsync(ct);
    }

    protected async override Task<WorkflowModel> ReadModel(string path, IDiagnostic diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        var parser = new MgxcParser(diag, prog)
        {
            Path = path,
            Assets = AssetManager,
        };
        return new WorkflowModel(await parser.ConvertAsync(ct));
    }
}