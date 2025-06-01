using PenguinTools.Attributes;
using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using mgxc = PenguinTools.Common.Chart.Models.mgxc;

namespace PenguinTools.Models;

[LocalizableCategoryOrder(nameof(Strings.Category_Song), 0, typeof(Strings))]
[LocalizableCategoryOrder(nameof(Strings.Category_Chart), 1, typeof(Strings))]
public class ChartModel(mgxc.Chart chart) : MetaModel
{
    [Browsable(false)]
    public override mgxc.Chart Chart { get; } = chart;

    [PropertyOrder(0)]
    [LocalizableCategory(nameof(Strings.Category_Song), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_SongId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_SongId), typeof(Strings))]
    public int? Id
    {
        get => Meta.Id;
        set => SetProperty(Meta.Id, value, newValue => Meta.Id = newValue, true);
    }

    // Chart
    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Designer), typeof(Strings))]
    [PropertyOrder(1)]
    public string Designer
    {
        get => Meta.Designer;
        set => SetProperty(Meta.Designer, value, x => Meta.Designer = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Difficulty), typeof(Strings))]
    [PropertyOrder(2)]
    public Difficulty Difficulty
    {
        get => Meta.Difficulty;
        set => SetProperty(Meta.Difficulty, value, x => Meta.Difficulty = x);
    }

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_DisplayBPM), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_DisplayBPM), typeof(Strings))]
    [PropertyOrder(3)]
    public decimal MainBpm => Meta.MainBpm;

    [LocalizableCategory(nameof(Strings.Category_Sync), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_InsertBlankMeasure), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_InsertBlankMeasure), typeof(Strings))]
    [PropertyOrder(0)]
    public bool BgmEnableBarOffset
    {
        get => Meta.BgmEnableBarOffset;
        set => SetProperty(Meta.BgmEnableBarOffset, value, x => Meta.BgmEnableBarOffset = x);
    }
}