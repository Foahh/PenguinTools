using PenguinTools.Attributes;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using Swordfish.NET.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using mgxc = PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Models;

public class BookDictionary : ConcurrentObservableSortedDictionary<int, Book>;

[LocalizableCategoryOrder(nameof(Strings.Category_Misc), 0, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Song), 1, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Chart), 2, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Display), 3, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_BGM), 4, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Sync), 5, typeof(Strings))]
public class Book : MetaModel
{
    [Browsable(false)]
    public ConcurrentObservableSortedDictionary<Difficulty, BookItem> Items { get; } = [];

    [Browsable(false)]
    public override mgxc.Chart Chart => Items[MainDifficulty].Chart;

    [Browsable(false)]
    public IEnumerable<Difficulty> AvailableDifficulties => Items.Keys;

    [PropertyOrder(-1)]
    [LocalizableCategory(nameof(Strings.Category_Misc), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_MainDifficulty), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_MainDifficulty), typeof(Strings))]
    public Difficulty MainDifficulty
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(string.Empty);
            foreach (var item in Items.Values) item.Meta.IsMain = item.Difficulty == value;
        }
    }

    [PropertyOrder(0)]
    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_SongId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_SongId), typeof(Strings))]
    public int? Id => Meta.Id;

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Title), typeof(Strings))]
    [PropertyOrder(1)]
    public string Title => Meta.Title;

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_SortName), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_SortName), typeof(Strings))]
    [PropertyOrder(2)]
    public string SortName => Meta.SortName;

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Artist), typeof(Strings))]
    [PropertyOrder(3)]
    public string Artist => Meta.Artist;

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Genre), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_Genre), typeof(Strings))]
    [PropertyOrder(4)]
    public Entry Genre => Meta.Genre;

    // Chart
    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Difficulty), typeof(Strings))]
    [PropertyOrder(0)]
    [Browsable(false)]
    public Difficulty Difficulty => Meta.Difficulty;

    [PropertyOrder(4)]
    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_UnlockEventID), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_UnlockEventID), typeof(Strings))]
    public int? UnlockEventId => Meta.UnlockEventId;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_WorldsEndTag), typeof(Strings))]
    [PropertyOrder(2)]
    public Entry WeTag => Meta.WeTag;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_WorldsEndDifficulty), typeof(Strings))]
    [PropertyOrder(3)]
    public StarDifficulty WeDifficulty => Meta.WeDifficulty;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ReleaseDate), typeof(Strings))]
    [PropertyOrder(4)]
    public DateTime ReleaseDate => Meta.ReleaseDate;

    // Display
    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_JacketFile), typeof(Strings))]
    [PropertyOrder(0)]
    public string JacketFilePath => Meta.JacketFilePath;

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_IsCustomStage), typeof(Strings))]
    [PropertyOrder(1)]
    public bool IsCustomStage => Meta.IsCustomStage;

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_StageId), typeof(Strings))]
    [PropertyOrder(2)]
    public int? StageId => Meta.StageId;

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BackgroundFile), typeof(Strings))]
    [PropertyOrder(3)]
    public string BgFilePath => Meta.BgiFilePath;

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_NotesFieldLine), typeof(Strings))]
    [PropertyOrder(4)]
    public Entry NotesFieldLine => Meta.NotesFieldLine;

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BackgroundStage), typeof(Strings))]
    [PropertyOrder(5)]
    public Entry Stage => Meta.Stage;

    // BGM
    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmFile), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_BgmFile), typeof(Strings))]
    [PropertyOrder(0)]
    public string BgmFilePath => Meta.BgmFilePath;

    // BGM
    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmPreviewStart), typeof(Strings))]
    [Range(0, int.MaxValue)]
    [PropertyOrder(1)]
    public decimal BgmPreviewStart => Meta.BgmPreviewStart;

    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmPreviewStop), typeof(Strings))]
    [Range(0, int.MaxValue)]
    [PropertyOrder(2)]
    public decimal BgmPreviewStop => Meta.BgmPreviewStop;

    // Sync
    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_RealOffset), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_RealOffset), typeof(Strings))]
    [PropertyOrder(0)]
    public decimal RealOffset => Meta.BgmRealOffset;

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ManualOffset), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_ManualOffset), typeof(Strings))]
    [PropertyOrder(1)]
    public decimal BgmOffset => Meta.BgmManualOffset;

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_InsertBlankMeasure), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_InsertBlankMeasure), typeof(Strings))]
    [PropertyOrder(2)]
    public bool BgmEnableBarOffset => Meta.BgmEnableBarOffset;

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmInitialBpm), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_BgmInitialBpm), typeof(Strings))]
    [Range(1, short.MaxValue)]
    [PropertyOrder(3)]
    public decimal BgmInitialBpm => Meta.BgmInitialBpm;

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmInitialTimeSignature), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_BgmInitialTimeSignature), typeof(Strings))]
    [PropertyOrder(4)]
    public TimeSignatureModel BgmInitialTimeSignature => new(Meta);
}