using PenguinTools.Attributes;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PenguinTools.Models;

[LocalizableCategoryOrder(nameof(Strings.Category_BGM), 0, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Sync), 1, typeof(Strings))]
public class MusicModel : Model
{
    public MusicModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(BgmOffset) && e.PropertyName != nameof(BgmEnableBarOffset) && e.PropertyName != nameof(BgmInitialBpm) && e.PropertyName != nameof(BgmInitialTimeSignature)) return;
            OnPropertyChanged(nameof(RealOffset));
        };
        BgmInitialTimeSignature = new TimeSignatureModel(Meta);
        BgmInitialTimeSignature.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(TimeSignatureModel.Numerator) && e.PropertyName != nameof(TimeSignatureModel.Denominator)) return;
            OnPropertyChanged(nameof(BgmInitialTimeSignature));
        };
    }

    [Browsable(false)]
    public Meta Meta { get; } = new();

    [PropertyOrder(-1)]
    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_SongId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_SongId), typeof(Strings))]
    public int? Id
    {
        get => Meta.Id;
        set => SetProperty(Meta.Id, value, newValue => Meta.Id = newValue, true);
    }

    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmPreviewStart), typeof(Strings))]
    [Range(0, int.MaxValue)]
    [PropertyOrder(1)]
    public decimal BgmPreviewStart
    {
        get => Meta.BgmPreviewStart;
        set => SetProperty(Meta.BgmPreviewStart, value, newValue => Meta.BgmPreviewStart = newValue, true);
    }

    [LocalizableCategory(nameof(Strings.Category_BGM), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BgmPreviewStop), typeof(Strings))]
    [Range(0, int.MaxValue)]
    [PropertyOrder(2)]
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
    public decimal RealOffset => Meta.BgmRealOffset;

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ManualOffset), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_ManualOffset), typeof(Strings))]
    [PropertyOrder(1)]
    public decimal BgmOffset
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
    [ExpandableObject]
    [PropertyOrder(4)]
    public TimeSignatureModel BgmInitialTimeSignature { get; }
}