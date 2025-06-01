using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Attributes;
using PenguinTools.Common.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PenguinTools.Models;

public partial class OptionModel : Model
{
    protected override string JsonName => "options.json";

    [ObservableProperty]
    [MinLength(4)]
    [MaxLength(4)]
    [PropertyOrder(0)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_OptionName), typeof(Strings))]
    public partial string OptionName { get; set; } = "AXXX";

    [ObservableProperty]
    [PropertyOrder(1)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ConvertChart), typeof(Strings))]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    public partial bool ConvertChart { get; set; } = true;
    
    [ObservableProperty]
    [PropertyOrder(2)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ConvertAudio), typeof(Strings))]
    public partial bool ConvertAudio { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(3)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ConvertJacket), typeof(Strings))]
    public partial bool ConvertJacket { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(4)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ConvertBackground), typeof(Strings))]
    public partial bool ConvertBackground { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(5)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_GenerateEventXml), typeof(Strings))]
    public partial bool GenerateEventXml { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(6)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ReleaseTagXml), typeof(Strings))]
    public partial bool GenerateReleaseTagXml { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(7)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_UltimaEventId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_UnlockEventIDOption), typeof(Strings))]
    public partial int UltimaEventId { get; set; } = 1000001;

    [ObservableProperty]
    [PropertyOrder(8)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_WeEventId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_UnlockEventIDOption), typeof(Strings))]
    public partial int WeEventId { get; set; } = 1000002;

    [ObservableProperty]
    [PropertyOrder(9)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BatchSize), typeof(Strings))]
    [Range(-1, int.MaxValue)]
    public partial int BatchSize { get; set; } = 8;

    [Browsable(false)]
    public string WorkingDirectory { get; set; } = string.Empty;

    [Browsable(false)]
    [JsonIgnore]
    public bool CanExecute => ConvertChart || ConvertAudio || ConvertJacket || ConvertBackground || GenerateEventXml;

    [ObservableProperty]
    [Browsable(false)]
    [JsonIgnore]
    public partial BookDictionary Books { get; set; } = new();
}