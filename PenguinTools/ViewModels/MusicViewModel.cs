using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Infrastructure;
using PenguinTools.Models;
using PenguinTools.Services;
using System.IO;

namespace PenguinTools.ViewModels;

public class MusicViewModel : WatchViewModel<MusicModel>
{
    public MusicViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
    }

    protected override Task<OperationResult<MusicModel>> ReadModel(string path, CancellationToken ct = default)
    {
        var model = new MusicModel();
        model.Meta.BgmFilePath = ModelPath;
        return Task.FromResult(OperationResult<MusicModel>.Success(model));
    }

    protected override bool CanRun()
    {
        return !string.IsNullOrWhiteSpace(ModelPath);
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
    {
        if (Model?.Id is null) throw new DiagnosticException(Strings.Error_Song_id_is_not_set);

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)ModelPath),
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return OperationResult.Success();
        var path = dlg.FolderName;

        var converter = new MusicConverter(
            new MusicConvertRequest(
                Model.Meta,
                path,
                AssetProvider.GetPath(InfrastructureAsset.DummyAcb),
                ResourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(Model.Meta.FullBgmFilePath)}.wav")),
            MediaTool);
        return await converter.ConvertAsync(ct);
    }
}
