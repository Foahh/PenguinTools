using PenguinTools.Attributes;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using System.ComponentModel;
using System.IO;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using mgxc = PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Models;

public class BookItem(mgxc.Chart chart) : MetaModel
{
    [Browsable(false)]
    public override mgxc.Chart Chart { get; } = chart;

    [Browsable(false)]
    public string FileName => Path.GetFileName(Meta.FilePath);

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_FilePath), typeof(Strings))]
    [PropertyOrder(0)]
    public string FilePath => Meta.FilePath;

    [PropertyOrder(1)]
    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_SongId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_SongId), typeof(Strings))]
    public int? Id => Meta.Id;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Designer), typeof(Strings))]
    [PropertyOrder(2)]
    public string Designer => Meta.Designer;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Difficulty), typeof(Strings))]
    [PropertyOrder(3)]
    public Difficulty Difficulty => Meta.Difficulty;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_Level), typeof(Strings))]
    [PropertyOrder(4)]
    public decimal Level => Meta.Level;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_DisplayBPM), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_DisplayBPM), typeof(Strings))]
    [PropertyOrder(5)]
    public decimal MainBpm => Meta.MainBpm;

    [LocalizableCategory(nameof(Strings.Category_Chart), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_MainTil), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_MainTil), typeof(Strings))]
    [PropertyOrder(6)]
    public int MainTil => Meta.MainTil;
}