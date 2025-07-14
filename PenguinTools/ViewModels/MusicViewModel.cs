using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Media;
using PenguinTools.Core.Resources;
using PenguinTools.Models;
using System.IO;

namespace PenguinTools.ViewModels;

public class MusicViewModel : WatchViewModel<MusicModel>
{
    protected override Task<MusicModel> ReadModel(string path, IDiagnostic d, IProgress<string> p, CancellationToken ct = default)
    {
        var model = new MusicModel();
        model.Meta.BgmFilePath = ModelPath;
        return Task.FromResult(model);
    }

    protected override bool CanRun()
    {
        return !string.IsNullOrWhiteSpace(ModelPath);
    }

    protected async override Task Action()
    {
        if (Model?.Id is null) throw new DiagnosticException(Strings.Error_song_id_is_not_set);

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)ModelPath),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FolderName;

        var converter = new MusicConverter();
        var opts = new MusicConverter.Context(Model.Meta, path);
        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(opts, diag, p, ct));
    }
}