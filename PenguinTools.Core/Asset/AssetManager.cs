using PenguinTools.Core.Resources;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PenguinTools.Core.Asset;

public class AssetManager : INotifyPropertyChanged
{
    public AssetManager()
    {
        MergeAssets = new AssetDictionary();
        HardAssets = new AssetDictionary(ResourceUtils.GetStream("assets.json"));
        PlusAssets = new AssetDictionary("assets.json");
        UserAssets = new AssetDictionary();
        Merge();
        NotifyAssetChanged();
    }

    // Asset Dictionary that merges all assets from various sources below
    public AssetDictionary MergeAssets { get; }

    // Assets embedded in the assembly, used for default values and initial setup.
    private AssetDictionary HardAssets { get; }

    // Assets loaded from the result of AssetDictionary.CollectAsync.
    private AssetDictionary PlusAssets { get; }

    // Assets from the user-defined.
    private AssetDictionary UserAssets { get; }

    public IReadOnlySet<Entry> this[AssetType type] => MergeAssets[type];
    public IReadOnlySet<Entry> GenreNames => MergeAssets.GenreNames;
    public IReadOnlySet<Entry> FieldLines => MergeAssets.FieldLines;
    public IReadOnlySet<Entry> StageNames => MergeAssets.StageNames;
    public IReadOnlySet<Entry> WeTagNames => MergeAssets.WeTagNames;

    public async Task CollectAssetsAsync(string workDir, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!Directory.Exists(workDir)) return;

        progress?.Report(Strings.Status_collecting);

        PlusAssets.MergeWith(await AssetDictionary.CollectAsync(workDir, ct));
        PlusAssets.SubtractWith(HardAssets);

        progress?.Report(Strings.Status_Saving);
        await PlusAssets.SaveAsync("assets.json", ct);

        Merge();
        NotifyAssetChanged();
    }

    private void Merge()
    {
        MergeAssets.Clear();
        MergeAssets.MergeWith(HardAssets, PlusAssets, UserAssets);
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