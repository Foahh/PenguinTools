using System.Diagnostics;
using System.Text.Json;
using PenguinTools.Assets;
using PenguinTools.CLI;
using PenguinTools.Core;
using PenguinTools.Core.Metadata;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Workflow;
using Xunit;
using UmgrChart = PenguinTools.Chart.Models.umgr.Chart;

namespace PenguinTools.Tests.Workflow;

public sealed class OptionExporterCacheTests
{
    [Fact]
    public async Task ExportAsync_SkipsJacket_WhenCachedInputsAndOutputsMatch()
    {
        var workPath = Path.Combine(Path.GetTempPath(), "PenguinToolsTests", Guid.NewGuid().ToString("N"));
        var outputPaths = ExportOutputPaths.FromOptionDirectory(Path.Combine(workPath, "AXXX"));
        var chartPath = Path.Combine(workPath, "chart.ugc");
        var jacketPath = Path.Combine(workPath, "jacket.png");
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(workPath);
        await File.WriteAllTextAsync(chartPath, "chart", ct);
        await File.WriteAllTextAsync(jacketPath, "jacket", ct);

        var cache = new OptionConversionCache();
        var settings = CreateSettings(true, false, cache);
        var mediaTool = new CountingMediaTool();
        using var resourceStore = new DummyResourceStore(workPath);
        var context = new MusicExportContext(
            TestAssets.Load(),
            mediaTool,
            resourceStore,
            new DummyInfrastructureAssetProvider(workPath));
        var meta = CreateMeta(workPath, chartPath) with
        {
            JacketFilePath = jacketPath
        };
        var book = CreateBook(meta, false);

        try
        {
            await OptionExporter.ExportAsync(context, settings, outputPaths, [book], workPath, ct);
            await OptionExporter.ExportAsync(context, settings, outputPaths, [book], workPath, ct);

            Assert.Equal(1, mediaTool.JacketConversions);
        }
        finally
        {
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
        }
    }

    [Fact]
    public async Task ExportAsync_ReconvertsJacket_WhenCachedOutputChanges()
    {
        var workPath = Path.Combine(Path.GetTempPath(), "PenguinToolsTests", Guid.NewGuid().ToString("N"));
        var outputPaths = ExportOutputPaths.FromOptionDirectory(Path.Combine(workPath, "AXXX"));
        var chartPath = Path.Combine(workPath, "chart.ugc");
        var jacketPath = Path.Combine(workPath, "jacket.png");
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(workPath);
        await File.WriteAllTextAsync(chartPath, "chart", ct);
        await File.WriteAllTextAsync(jacketPath, "jacket", ct);

        var cache = new OptionConversionCache();
        var settings = CreateSettings(true, false, cache);
        var mediaTool = new CountingMediaTool();
        using var resourceStore = new DummyResourceStore(workPath);
        var context = new MusicExportContext(
            TestAssets.Load(),
            mediaTool,
            resourceStore,
            new DummyInfrastructureAssetProvider(workPath));
        var meta = CreateMeta(workPath, chartPath) with
        {
            JacketFilePath = jacketPath
        };
        var book = CreateBook(meta, false);
        var jacketOutputPath = Path.Combine(outputPaths.MusicFolder, "music4321", "CHU_UI_Jacket_4321.dds");

        try
        {
            await OptionExporter.ExportAsync(context, settings, outputPaths, [book], workPath, ct);
            await File.WriteAllTextAsync(jacketOutputPath, "edited output", ct);
            await OptionExporter.ExportAsync(context, settings, outputPaths, [book], workPath, ct);

            Assert.Equal(2, mediaTool.JacketConversions);
        }
        finally
        {
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
        }
    }

    [Fact]
    public async Task ExportAsync_SkipsStage_WhenCachedInputsAndOutputsMatch()
    {
        var workPath = Path.Combine(Path.GetTempPath(), "PenguinToolsTests", Guid.NewGuid().ToString("N"));
        var outputPaths = ExportOutputPaths.FromOptionDirectory(Path.Combine(workPath, "AXXX"));
        var chartPath = Path.Combine(workPath, "chart.ugc");
        var backgroundPath = Path.Combine(workPath, "background.png");
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(workPath);
        await File.WriteAllTextAsync(chartPath, "chart", ct);
        await File.WriteAllTextAsync(backgroundPath, "background", ct);

        var cache = new OptionConversionCache();
        var settings = CreateSettings(false, true, cache);
        var mediaTool = new CountingMediaTool();
        using var resourceStore = new DummyResourceStore(workPath);
        var context = new MusicExportContext(
            TestAssets.Load(),
            mediaTool,
            resourceStore,
            new DummyInfrastructureAssetProvider(workPath));
        var meta = CreateMeta(workPath, chartPath) with
        {
            BgiFilePath = backgroundPath
        };
        var book = CreateBook(meta, true);

        try
        {
            await OptionExporter.ExportAsync(context, settings, outputPaths, [book], workPath, ct);
            await OptionExporter.ExportAsync(context, settings, outputPaths, [book], workPath, ct);

            Assert.Equal(1, mediaTool.StageConversions);
        }
        finally
        {
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
        }
    }

    [Fact]
    public async Task AudioCache_HitsOnlyWhenInputsAndOutputsMatch()
    {
        var workPath = Path.Combine(Path.GetTempPath(), "PenguinToolsTests", Guid.NewGuid().ToString("N"));
        var chartPath = Path.Combine(workPath, "chart.ugc");
        var audioPath = Path.Combine(workPath, "audio.wav");
        var dummyAcbPath = Path.Combine(workPath, "dummy.acb");
        var cueFileFolder = Path.Combine(workPath, "cueFile");
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(workPath);
        await File.WriteAllTextAsync(chartPath, "chart", ct);
        await File.WriteAllTextAsync(audioPath, "audio", ct);
        await File.WriteAllTextAsync(dummyAcbPath, "dummy", ct);

        var meta = CreateMeta(workPath, chartPath) with
        {
            BgmFilePath = audioPath
        };

        try
        {
            var cache = new OptionConversionCache();
            var cachedConversion = await OptionConversionCacheArtifacts.CreateAudioAsync(
                meta,
                cueFileFolder,
                dummyAcbPath,
                ct);
            await WriteOutputsAsync(cachedConversion.Outputs, "output", ct);

            await OptionConversionCacheValidator.StoreAsync(
                cache,
                cachedConversion.Key,
                cachedConversion.State,
                cachedConversion.Outputs,
                ct);

            Assert.True(await OptionConversionCacheValidator.IsHitAsync(
                cache,
                cachedConversion.Key,
                cachedConversion.State,
                cachedConversion.Outputs,
                ct));

            await File.WriteAllTextAsync(audioPath, "changed audio", ct);
            var changedInput = await OptionConversionCacheArtifacts.CreateAudioAsync(
                meta,
                cueFileFolder,
                dummyAcbPath,
                ct);

            Assert.False(await OptionConversionCacheValidator.IsHitAsync(
                cache,
                changedInput.Key,
                changedInput.State,
                changedInput.Outputs,
                ct));
        }
        finally
        {
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
        }
    }

    [Fact]
    public async Task AudioCache_MissesWhenOutputChanges()
    {
        var workPath = Path.Combine(Path.GetTempPath(), "PenguinToolsTests", Guid.NewGuid().ToString("N"));
        var chartPath = Path.Combine(workPath, "chart.ugc");
        var audioPath = Path.Combine(workPath, "audio.wav");
        var dummyAcbPath = Path.Combine(workPath, "dummy.acb");
        var cueFileFolder = Path.Combine(workPath, "cueFile");
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(workPath);
        await File.WriteAllTextAsync(chartPath, "chart", ct);
        await File.WriteAllTextAsync(audioPath, "audio", ct);
        await File.WriteAllTextAsync(dummyAcbPath, "dummy", ct);

        var meta = CreateMeta(workPath, chartPath) with
        {
            BgmFilePath = audioPath
        };

        try
        {
            var cache = new OptionConversionCache();
            var cachedConversion = await OptionConversionCacheArtifacts.CreateAudioAsync(
                meta,
                cueFileFolder,
                dummyAcbPath,
                ct);
            await WriteOutputsAsync(cachedConversion.Outputs, "output", ct);
            await OptionConversionCacheValidator.StoreAsync(
                cache,
                cachedConversion.Key,
                cachedConversion.State,
                cachedConversion.Outputs,
                ct);

            await File.WriteAllTextAsync(cachedConversion.Outputs[0].Path, "edited output", ct);

            Assert.False(await OptionConversionCacheValidator.IsHitAsync(
                cache,
                cachedConversion.Key,
                cachedConversion.State,
                cachedConversion.Outputs,
                ct));
        }
        finally
        {
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
        }
    }

    [Fact]
    public void OptionDocumentJson_RoundTripsConversionCache()
    {
        var document = new OptionDocument
        {
            ConversionCache = new OptionConversionCache()
        };
        document.ConversionCache.SetEntry("audio:4321", new OptionConversionCacheEntry
        {
            RecipeHash = "ABC",
            Inputs = new Dictionary<string, string>
            {
                ["bgm"] = "123"
            },
            Outputs = new Dictionary<string, string>
            {
                ["awb"] = "456"
            }
        });

        var json = JsonSerializer.Serialize(document, OptionDocumentJson.Default);
        var roundTripped = JsonSerializer.Deserialize<OptionDocument>(json, OptionDocumentJson.Default);

        Assert.NotNull(roundTripped);
        var entry = roundTripped.ConversionCache.GetEntry("audio:4321");
        Assert.NotNull(entry);
        Assert.Equal("ABC", entry.RecipeHash);
        Assert.Equal("123", entry.Inputs["bgm"]);
        Assert.Equal("456", entry.Outputs["awb"]);
    }

    [Fact]
    public void CliOptionDocumentJson_RoundTripsConversionCache()
    {
        var document = new OptionDocument
        {
            ConversionCache = new OptionConversionCache()
        };
        document.ConversionCache.SetEntry("jacket:music4321", new OptionConversionCacheEntry
        {
            RecipeHash = "ABC",
            Inputs = new Dictionary<string, string>
            {
                ["jacket"] = "123"
            },
            Outputs = new Dictionary<string, string>
            {
                ["jacketDds"] = "456"
            }
        });

        var json = JsonSerializer.Serialize(document, CliJsonSerializerContext.Default.OptionDocument);
        var roundTripped = JsonSerializer.Deserialize(json, CliJsonSerializerContext.Default.OptionDocument);

        Assert.NotNull(roundTripped);
        var entry = roundTripped.ConversionCache.GetEntry("jacket:music4321");
        Assert.NotNull(entry);
        Assert.Equal("ABC", entry.RecipeHash);
        Assert.Equal("123", entry.Inputs["jacket"]);
        Assert.Equal("456", entry.Outputs["jacketDds"]);
    }

    private static OptionExportSettings CreateSettings(
        bool convertJacket,
        bool convertBackground,
        OptionConversionCache cache)
    {
        return new OptionExportSettings(
            false,
            convertJacket,
            false,
            convertBackground,
            false,
            123,
            "My Pack",
            false,
            1000001,
            1000002,
            1,
            cache);
    }

    private static Meta CreateMeta(string workPath, string chartPath)
    {
        return new Meta
        {
            Id = 4321,
            Title = "Test Song",
            SortName = "Test Song",
            Artist = "Tester",
            Difficulty = Difficulty.Master,
            FilePath = chartPath,
            BgmFilePath = Path.Combine(workPath, "audio.wav")
        };
    }

    private static OptionBookSnapshot CreateBook(Meta meta, bool isCustomStage)
    {
        return new OptionBookSnapshot(
            meta,
            isCustomStage,
            54321,
            meta.NotesFieldLine,
            meta.Stage,
            meta.Title,
            new Dictionary<Difficulty, OptionDifficultySnapshot>
            {
                [Difficulty.Master] = new(Difficulty.Master, meta.Id, new UmgrChart(), meta)
            });
    }

    private static async Task WriteOutputsAsync(
        IReadOnlyList<OptionConversionArtifact> outputs,
        string content,
        CancellationToken ct)
    {
        foreach (var output in outputs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output.Path)!);
            await File.WriteAllTextAsync(output.Path, $"{output.Name}:{content}", ct);
        }
    }

    private sealed class CountingMediaTool : IMediaTool
    {
        public int JacketConversions { get; private set; }
        public int StageConversions { get; private set; }

        public Task<ProcessCommandResult> NormalizeAudioAsync(string src, string dst, decimal offset,
            CancellationToken ct = default)
        {
            return Task.FromResult(Ok());
        }

        public Task<ProcessCommandResult> CheckAudioValidAsync(string src, CancellationToken ct = default)
        {
            return Task.FromResult(Ok());
        }

        public Task<ProcessCommandResult> CheckImageValidAsync(string src, CancellationToken ct = default)
        {
            return Task.FromResult(Ok());
        }

        public async Task ConvertJacketAsync(string src, string dst, CancellationToken ct = default)
        {
            JacketConversions++;
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            await File.WriteAllTextAsync(dst, await File.ReadAllTextAsync(src, ct), ct);
        }

        public async Task ConvertStageAsync(string bg, string stSrc, string stDst, string?[]? fxPaths,
            CancellationToken ct = default)
        {
            StageConversions++;
            Directory.CreateDirectory(Path.GetDirectoryName(stDst)!);
            await File.WriteAllTextAsync(stDst, $"{await File.ReadAllTextAsync(bg, ct)}:{stSrc}", ct);
        }

        public Task ExtractDdsAsync(string src, string dst, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        private static ProcessCommandResult Ok()
        {
            return new ProcessCommandResult(
                new ProcessStartInfo { FileName = "null" },
                (int)InterExitCode.Success,
                "",
                "");
        }
    }

    private sealed class DummyInfrastructureAssetProvider : IInfrastructureAssetProvider
    {
        private readonly string _dummyAcbPath;
        private readonly string _notesFieldTemplatePath;
        private readonly string _stageTemplatePath;

        public DummyInfrastructureAssetProvider(string workPath)
        {
            _dummyAcbPath = Path.Combine(workPath, "dummy.acb");
            _stageTemplatePath = Path.Combine(workPath, "stage-template.afb");
            _notesFieldTemplatePath = Path.Combine(workPath, "notes-field-template.afb");

            File.WriteAllText(_dummyAcbPath, "dummy");
            File.WriteAllText(_stageTemplatePath, "stage-template");
            File.WriteAllText(_notesFieldTemplatePath, "notes-field-template");
        }

        public string GetPath(InfrastructureAsset asset)
        {
            return asset switch
            {
                InfrastructureAsset.DummyAcb => _dummyAcbPath,
                InfrastructureAsset.StageTemplate => _stageTemplatePath,
                InfrastructureAsset.NotesFieldTemplate => _notesFieldTemplatePath,
                _ => throw new ArgumentOutOfRangeException(nameof(asset), asset, null)
            };
        }
    }

    private sealed class DummyResourceStore(string tempWorkPath) : IResourceStore
    {
        public string TempWorkPath { get; } = tempWorkPath;

        public bool HasResource(string resourceName)
        {
            return false;
        }

        public string GetTempPath(string fileName)
        {
            Directory.CreateDirectory(TempWorkPath);
            return Path.Combine(TempWorkPath, fileName);
        }

        public string ExtractToTemp(string resourceName)
        {
            return GetTempPath(resourceName);
        }

        public Task CopyToAsync(string resourceName, string destinationPath, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Stream OpenRead(string resourceName)
        {
            throw new FileNotFoundException(resourceName);
        }

        public void Dispose()
        {
        }
    }
}