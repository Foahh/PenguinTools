using PenguinTools.Attributes;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using mgxc = PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Models;

[LocalizableCategoryOrder(nameof(Strings.Category_Song), 0, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Chart), 1, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Display), 2, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_BGM), 3, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Sync), 4, typeof(Strings))]
public class WorkflowModel : MetaModel
{
    public WorkflowModel(mgxc.Chart chart)
    {
        Chart = chart;
        BgmInitialTimeSignature = new TimeSignatureModel(Meta);
    }

    [Browsable(false)]
    public override mgxc.Chart Chart { get; }

    [PropertyOrder(0)]
    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_SongId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_SongId), typeof(Strings))]
    public int? Id
    {
        get => Meta.Id;
        set => SetProperty(Meta.Id, value, newValue => Meta.Id = newValue, true);
    }

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Title), typeof(Strings))]
    [PropertyOrder(1)]
    public string Title
    {
        get => Meta.Title;
        set => SetProperty(Meta.Title, value, x => Meta.Title = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_SortName), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_SortName), typeof(Strings))]
    [PropertyOrder(2)]
    public string SortName
    {
        get => Meta.SortName;
        set => SetProperty(Meta.SortName, value, x => Meta.SortName = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Artist), typeof(Strings))]
    [PropertyOrder(3)]
    public string Artist
    {
        get => Meta.Artist;
        set => SetProperty(Meta.Artist, value, x => Meta.Artist = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Genre), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_Genre), typeof(Strings))]
    [PropertyOrder(4)]
    public Entry Genre
    {
        get => Meta.Genre;
        set => SetProperty(Meta.Genre, value, x => Meta.Genre = x);
    }

    // Chart
    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Designer), typeof(Strings))]
    [PropertyOrder(0)]
    public string Designer
    {
        get => Meta.Designer;
        set => SetProperty(Meta.Designer, value, x => Meta.Designer = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Difficulty), typeof(Strings))]
    [PropertyOrder(1)]
    public Difficulty Difficulty
    {
        get => Meta.Difficulty;
        set => SetProperty(Meta.Difficulty, value, x => Meta.Difficulty = x);
    }

    [PropertyOrder(2)]
    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_UnlockEventID), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_UnlockEventID), typeof(Strings))]
    public int? UnlockEventId
    {
        get => Meta.UnlockEventId;
        set => SetProperty(Meta.UnlockEventId, value, x => Meta.UnlockEventId = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_WorldsEndTag), typeof(Strings))]
    [PropertyOrder(3)]
    public Entry WeTag
    {
        get => Meta.WeTag;
        set => SetProperty(Meta.WeTag, value, x => Meta.WeTag = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_WorldsEndDifficulty), typeof(Strings))]
    [PropertyOrder(4)]
    public StarDifficulty WeDifficulty
    {
        get => Meta.WeDifficulty;
        set => SetProperty(Meta.WeDifficulty, value, x => Meta.WeDifficulty = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Level), typeof(Strings))]
    [PropertyOrder(5)]
    public decimal Level
    {
        get => Meta.Level;
        set => SetProperty(Meta.Level, value, x => Meta.Level = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ReleaseDate), typeof(Strings))]
    [PropertyOrder(6)]
    public DateTime ReleaseDate
    {
        get => Meta.ReleaseDate;
        set => SetProperty(Meta.ReleaseDate, value, x => Meta.ReleaseDate = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_DisplayBPM), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_DisplayBPM), typeof(Strings))]
    [PropertyOrder(7)]
    public decimal MainBpm
    {
        get => Meta.MainBpm;
        set => SetProperty(Meta.MainBpm, value, x => Meta.MainBpm = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_MainTil), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_MainTil), typeof(Strings))]
    [PropertyOrder(8)]
    public int MainTil
    {
        get => Meta.MainTil;
        set => SetProperty(Meta.MainTil, value, x => Meta.MainTil = x);
    }

    // Display
    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_JacketFile), typeof(Strings))]
    [PropertyOrder(0)]
    public string JacketFilePath
    {
        get => Meta.JacketFilePath;
        set => SetProperty(Meta.JacketFilePath, value, x => Meta.JacketFilePath = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_IsCustomStage), typeof(Strings))]
    [PropertyOrder(1)]
    [ReadOnly(false)]
    public bool IsCustomStage
    {
        get => Meta.IsCustomStage;
        set => SetProperty(Meta.IsCustomStage, value, x => Meta.IsCustomStage = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_StageId), typeof(Strings))]
    [PropertyOrder(2)]
    public int? StageId
    {
        get => Meta.StageId;
        set => SetProperty(Meta.StageId, value, x => Meta.StageId = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BackgroundFile), typeof(Strings))]
    [PropertyOrder(3)]
    public string BgFilePath
    {
        get => Meta.BgiFilePath;
        set => SetProperty(Meta.BgiFilePath, value, x => Meta.BgiFilePath = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_NotesFieldLine), typeof(Strings))]
    [PropertyOrder(4)]
    public Entry NotesFieldLine
    {
        get => Meta.NotesFieldLine;
        set => SetProperty(Meta.NotesFieldLine, value, x => Meta.NotesFieldLine = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Display), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BackgroundStage), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_BackgroundStage), typeof(Strings))]
    [PropertyOrder(5)]
    [ReadOnly(false)]
    public Entry Stage
    {
        get => Meta.Stage;
        set => SetProperty(Meta.Stage, value, x => Meta.Stage = x);
    }

    // BGM
    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmFile), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_BgmFile), typeof(Strings))]
    [PropertyOrder(0)]
    public string BgmFilePath
    {
        get => Meta.BgmFilePath;
        set => SetProperty(Meta.BgmFilePath, value, x => Meta.BgmFilePath = x);
    }

    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmPreviewStart), typeof(Strings))]
    [Range(0, int.MaxValue)]
    [PropertyOrder(2)]
    public decimal BgmPreviewStart
    {
        get => Meta.BgmPreviewStart;
        set => SetProperty(Meta.BgmPreviewStart, value, newValue => Meta.BgmPreviewStart = newValue, true);
    }

    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmPreviewStop), typeof(Strings))]
    [Range(0, int.MaxValue)]
    [PropertyOrder(3)]
    public decimal BgmPreviewStop
    {
        get => Meta.BgmPreviewStop;
        set => SetProperty(Meta.BgmPreviewStop, value, newValue => Meta.BgmPreviewStop = newValue, true);
    }

    // Sync
    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_RealOffset), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_RealOffset), typeof(Strings))]
    [PropertyOrder(0)]
    public decimal BgmRealOffset => Meta.BgmRealOffset;

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ManualOffset), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_ManualOffset), typeof(Strings))]
    [PropertyOrder(1)]
    public decimal BgmManualOffset
    {
        get => Meta.BgmManualOffset;
        set => SetProperty(Meta.BgmManualOffset, value, newValue => Meta.BgmManualOffset = newValue);
    }

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_InsertBlankMeasure), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_InsertBlankMeasure), typeof(Strings))]
    [PropertyOrder(2)]
    public bool BgmEnableBarOffset
    {
        get => Meta.BgmEnableBarOffset;
        set => SetProperty(Meta.BgmEnableBarOffset, value, newValue => Meta.BgmEnableBarOffset = newValue);
    }

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmInitialBpm), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_BgmInitialBpm), typeof(Strings))]
    [Range(1, short.MaxValue)]
    [PropertyOrder(3)]
    public decimal BgmInitialBpm
    {
        get => Meta.BgmInitialBpm;
        set => SetProperty(Meta.BgmInitialBpm, value, newValue => Meta.BgmInitialBpm = newValue, true);
    }

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmInitialTimeSignature), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_BgmInitialTimeSignature), typeof(Strings))]
    [PropertyOrder(4)]
    public TimeSignatureModel BgmInitialTimeSignature { get; }
}