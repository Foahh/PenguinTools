using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Chart;
using PenguinTools.Core.Chart.Converter;
using PenguinTools.Core.Chart.Parser;
using PenguinTools.Core.Media;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;
using PenguinTools.Models;
using System.IO;
using System.Media;

namespace PenguinTools.ViewModels;

public class WorkflowViewModel : WatchViewModel<WorkflowModel>
{
    protected async override Task Action()
    {
        if (Model == null) return;
        var chart = Model.Chart;
        var meta = chart.Meta;
        var songId = meta.Id ?? throw new DiagnosticException(Strings.Error_song_id_is_not_set);
        if (string.IsNullOrWhiteSpace(meta.FullBgmFilePath)) throw new DiagnosticException(Strings.Error_audio_file_is_not_set);
        if (string.IsNullOrWhiteSpace(meta.FullJacketFilePath)) throw new DiagnosticException(Strings.Error_jacket_file_is_not_set);
        if (meta.IsCustomStage)
        {
            if (string.IsNullOrWhiteSpace(meta.FullBgiFilePath)) throw new DiagnosticException(Strings.Error_background_file_is_not_set);
            if (meta.StageId is null) throw new DiagnosticException(Strings.Error_stage_id_is_not_set);
        }

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)ModelPath),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FolderName;

        await ActionService.RunAsync(async (diag, p, ct) =>
        {
            var stage = meta.Stage;
            if (meta.IsCustomStage)
            {
                var stageConverter = new StageConverter(AssetManager);
                var stageOpts = new StageConverter.Context(meta.FullBgiFilePath, [], meta.StageId, path, meta.NotesFieldLine);
                await stageConverter.ConvertAsync(stageOpts, diag, p, ct);
                if (diag.HasError) return;
                ct.ThrowIfCancellationRequested();
                stage = stageOpts.Result;
            }

            var metaMap = new Dictionary<Difficulty, Meta> { [meta.Difficulty] = meta };
            var xml = new MusicXml(metaMap, meta.Difficulty) { StageName = stage };

            if (meta is { Difficulty: Difficulty.WorldsEnd or Difficulty.Ultima, UnlockEventId: { } eventId })
            {
                var type = meta.Difficulty == Difficulty.WorldsEnd ? EventXml.MusicType.WldEnd : EventXml.MusicType.Ultima;
                var eXml = new EventXml(eventId, type, [new Entry(songId, meta.Title)]);
                await eXml.SaveDirectoryAsync(path);
            }

            var musicFolder = await xml.SaveDirectoryAsync(path);

            var chartPath = Path.Combine(musicFolder, xml[meta.Difficulty].File);
            var chartOpts = new ChartConverter.Context(chartPath, chart);
            var musicOpts = new MusicConverter.Context(Model.Meta, path);
            var jacketOpts = new JacketConverter.Context(meta.FullJacketFilePath, Path.Combine(musicFolder, xml.JaketFile));

            var chartConverter = new ChartConverter();
            await chartConverter.ConvertAsync(chartOpts, diag, p, ct);
            if (diag.HasError) return;
            ct.ThrowIfCancellationRequested();

            var jacketConverter = new JacketConverter();
            await jacketConverter.ConvertAsync(jacketOpts, diag, p, ct);
            if (diag.HasError) return;
            ct.ThrowIfCancellationRequested();

            var musicConverter = new MusicConverter();
            await musicConverter.ConvertAsync(musicOpts, diag, p, ct);
            if (diag.HasError) return;
            ct.ThrowIfCancellationRequested();
        });

        SystemSounds.Exclamation.Play();
    }

    protected async override Task<WorkflowModel> ReadModel(string path, IDiagnostic d, IProgress<string> p, CancellationToken ct = default)
    {
        var parser = new MgxcParser(d, AssetManager);
        var chart = await parser.ParseAsync(path, ct);
        return new WorkflowModel(chart);
    }
}