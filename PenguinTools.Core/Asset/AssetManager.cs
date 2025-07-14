using PenguinTools.Core.Resources;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PenguinTools.Core.Asset;

public class AssetManager : INotifyPropertyChanged
{
    private const string PATH = "assets.json";

    public AssetManager()
    {
        MergeAssets.MergeWith(HardAssets, new AssetDictionary(PATH));
        NotifyAssetChanged();
    }

    public AssetDictionary MergeAssets { get; private set; } = [];
    private AssetDictionary HardAssets { get; } = new(ResourceUtils.GetStream("assets.json"));
    private AssetDictionary UserAssets { get; } = [];
    
    public IReadOnlySet<Entry> this[AssetType type] => MergeAssets[type];
    public IReadOnlySet<Entry> GenreNames => MergeAssets.GenreNames;
    public IReadOnlySet<Entry> FieldLines => MergeAssets.FieldLines;
    public IReadOnlySet<Entry> StageNames => MergeAssets.StageNames;
    public IReadOnlySet<Entry> WeTagNames => MergeAssets.WeTagNames;

    public async Task CollectAssetsAsync(string workDir, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!Directory.Exists(workDir)) return;

        progress?.Report(Strings.Status_collecting);
        var collected = await AssetDictionary.CollectAsync(workDir, ct);
        progress?.Report(Strings.Status_saving);
        await collected.SaveAsync(PATH, ct);

        MergeAssets = [];
        MergeAssets.MergeWith(HardAssets, UserAssets, collected);
        NotifyAssetChanged();
    }

    public void DefineEntry(AssetType type, Entry entry)
    {
        UserAssets[type].Add(entry);
        MergeAssets[type].Add(entry);
        NotifyAssetChanged(type);
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void NotifyAssetChanged(AssetType? type = null)
    {
        OnPropertyChanged(nameof(MergeAssets));

        if (type is not null)
        {
            OnPropertyChanged(type.ToString());
            return;
        }

        OnPropertyChanged(nameof(GenreNames));
        OnPropertyChanged(nameof(FieldLines));
        OnPropertyChanged(nameof(StageNames));
        OnPropertyChanged(nameof(WeTagNames));
    }

    #endregion
}