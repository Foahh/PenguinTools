using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Attributes;
using PenguinTools.Resources;
using PenguinTools.Workflow;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PenguinTools.Models;

public partial class OptionModel : Model, IPersistable
{
    public string PersistenceFileName => "options.json";

    public async Task LoadAsync(string directory, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(directory, PersistenceFileName);
        if (!File.Exists(path)) return;

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<OptionDocument>(stream, OptionDocumentJson.Default, cancellationToken);
        if (document is null) return;

        Apply(document);
    }

    public async Task SaveAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentNullException(nameof(directory));
        var path = Path.Combine(directory, PersistenceFileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, ToDocument(), OptionDocumentJson.Default, cancellationToken);
    }

    public void Apply(OptionDocument document)
    {
        OptionName = document.OptionName;
        ConvertChart = document.ConvertChart;
        ChartFileDiscovery = document.ChartFileDiscovery;
        ConvertAudio = document.ConvertAudio;
        ConvertJacket = document.ConvertJacket;
        ConvertBackground = document.ConvertBackground;
        GenerateEventXml = document.GenerateEventXml;
        GenerateReleaseTagXml = document.GenerateReleaseTagXml;
        UltimaEventId = document.UltimaEventId;
        WeEventId = document.WeEventId;
        BatchSize = document.BatchSize;
        WorkingDirectory = document.WorkingDirectory;
    }

    public OptionDocument ToDocument() =>
        new()
        {
            OptionName = OptionName,
            ConvertChart = ConvertChart,
            ChartFileDiscovery = ChartFileDiscovery,
            ConvertAudio = ConvertAudio,
            ConvertJacket = ConvertJacket,
            ConvertBackground = ConvertBackground,
            GenerateEventXml = GenerateEventXml,
            GenerateReleaseTagXml = GenerateReleaseTagXml,
            UltimaEventId = UltimaEventId,
            WeEventId = WeEventId,
            BatchSize = BatchSize,
            WorkingDirectory = WorkingDirectory
        };

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
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ChartFileDiscovery), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_ChartFileDiscovery), typeof(Strings))]
    public partial ChartFileDiscoveryMode ChartFileDiscovery { get; set; } = ChartFileDiscoveryMode.MgxcOnly;

    [ObservableProperty]
    [PropertyOrder(3)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ConvertAudio), typeof(Strings))]
    public partial bool ConvertAudio { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(4)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ConvertJacket), typeof(Strings))]
    public partial bool ConvertJacket { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(5)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ConvertBackground), typeof(Strings))]
    public partial bool ConvertBackground { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(6)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_GenerateEventXml), typeof(Strings))]
    public partial bool GenerateEventXml { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(7)]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_ReleaseTagXml), typeof(Strings))]
    public partial bool GenerateReleaseTagXml { get; set; } = true;

    [ObservableProperty]
    [PropertyOrder(8)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_UltimaEventId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_UnlockEventIDOption), typeof(Strings))]
    public partial int UltimaEventId { get; set; } = 1000001;

    [ObservableProperty]
    [PropertyOrder(9)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_WeEventId), typeof(Strings))]
    [LocalizableDescription(nameof(Strings.Description_UnlockEventIDOption), typeof(Strings))]
    public partial int WeEventId { get; set; } = 1000002;

    [ObservableProperty]
    [PropertyOrder(10)]
    [LocalizableCategory(nameof(Strings.Category_Settings), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_BatchSize), typeof(Strings))]
    [Range(-1, int.MaxValue)]
    public partial int BatchSize { get; set; } = 8;

    [Browsable(false)]
    public string WorkingDirectory { get; set; } = string.Empty;

    [LocalizableCategory(nameof(Strings.Category_Misc), typeof(Strings))]
    [LocalizableDisplayName(nameof(Strings.Display_LastExport), typeof(Strings))]
    public string OptionDirectory
    {
        get
        {
            var folder = Path.GetFileName(WorkingDirectory);
            return folder == OptionName ? WorkingDirectory : Path.Combine(WorkingDirectory, OptionName);
        }
    }

    [Browsable(false)]
    [JsonIgnore]
    public bool CanExecute => ConvertChart || ConvertAudio || ConvertJacket || ConvertBackground || GenerateEventXml;

    [ObservableProperty]
    [Browsable(false)]
    [JsonIgnore]
    public partial BookDictionary Books { get; set; } = new();
}