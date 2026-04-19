using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Infrastructure;
using PenguinTools.Services;
using System.IO;

namespace PenguinTools.ViewModels;

public partial class JacketViewModel : ActionViewModel
{
    public JacketViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DestinationFileName))]
    public partial int? JacketId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DestinationFileName))]
    [NotifyCanExecuteChangedFor(nameof(ActionCommand))]
    public partial string JacketPath { get; set; } = string.Empty;

    public string DestinationFileName
    {
        get
        {
            if (JacketId is { } id) return $"[CHU_UI_Jacket_{id:0000}.dds]";
            return !string.IsNullOrWhiteSpace(JacketPath) ? $"[CHU_UI_Jacket_{Path.GetFileNameWithoutExtension((string?)JacketPath)}.dds]" : string.Empty;
        }
    }

    protected override bool CanRun()
    {
        return !string.IsNullOrWhiteSpace(JacketPath);
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
    {
        var fileName = JacketId is null ? Path.GetFileNameWithoutExtension((string?)JacketPath) : $"{(int)JacketId:0000}";
        var dlg = new SaveFileDialog
        {
            Filter = Strings.Filefilter_dds,
            FileName = $"CHU_UI_Jacket_{fileName}"
        };
        if (dlg.ShowDialog() != true) return OperationResult.Success();

        var converter = new JacketConverter(new JacketConvertRequest(JacketPath, dlg.FileName), MediaTool);
        return await converter.ConvertAsync(ct);
    }
}
