using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PenguinTools.Core.Asset;

public class AssetManager : INotifyPropertyChanged
{
    public const string PlusAssetsFileName = "assets.user.json";

    private readonly string _plusAssetsPath;

    public AssetManager(Stream hardAssets, string userDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(hardAssets);
        ArgumentException.ThrowIfNullOrWhiteSpace(userDataDirectory);

        Directory.CreateDirectory(userDataDirectory);
        _plusAssetsPath = Path.Combine(userDataDirectory, PlusAssetsFileName);

        MergeAssets = new AssetDictionary();
        HardAssets = new AssetDictionary(hardAssets);
        var plusOk = AssetDictionary.TryLoadPlusAssetsFromFile(_plusAssetsPath, out var plus);
        PlusAssets = plus;
        ShouldPromptForOptionalAssetsImport = !plusOk;
        UserAssets = new AssetDictionary();
        Merge();
        NotifyAssetChanged();
    }

    /// <summary>True when <see cref="PlusAssetsFileName"/> was missing or not valid JSON at startup.</summary>
    public bool ShouldPromptForOptionalAssetsImport { get; }

    /// <summary>Absolute path to the merged plus-tier asset JSON on disk.</summary>
    public string PlusAssetsPath => _plusAssetsPath;

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

    public async Task CollectAssetsAsync(string workDir, CancellationToken ct = default)
    {
        if (!Directory.Exists(workDir)) { return; }

        PlusAssets.MergeWith(await AssetDictionary.CollectAsync(workDir, ct));
        PlusAssets.SubtractWith(HardAssets);

        await PlusAssets.SaveAsync(_plusAssetsPath, ct);

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