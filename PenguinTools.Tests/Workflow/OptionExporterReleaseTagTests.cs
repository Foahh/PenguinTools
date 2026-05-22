using System.Xml.Linq;
using PenguinTools.Assets;
using PenguinTools.Core;
using PenguinTools.Core.Metadata;
using PenguinTools.Infrastructure;
using PenguinTools.Workflow;
using Xunit;
using UmgrChart = PenguinTools.Chart.Models.umgr.Chart;

namespace PenguinTools.Tests.Workflow;

public sealed class OptionExporterReleaseTagTests
{
    [Fact]
    public async Task ExportAsync_WritesConfiguredReleaseTagToEachMusicXml()
    {
        var workPath = Path.Combine(Path.GetTempPath(), "PenguinToolsTests", Guid.NewGuid().ToString("N"));
        var outputPaths = ExportOutputPaths.FromOptionDirectory(Path.Combine(workPath, "AXXX"));
        var settings = new OptionExportSettings(
            false,
            true,
            false,
            false,
            false,
            123,
            "My Pack",
            false,
            1000001,
            1000002,
            1);
        var meta = new Meta
        {
            Id = 4321,
            Title = "Test Song",
            SortName = "Test Song",
            Artist = "Tester",
            Difficulty = Difficulty.Master,
            FilePath = Path.Combine(workPath, "chart.ugc")
        };
        var book = new OptionBookSnapshot(
            meta,
            false,
            null,
            meta.NotesFieldLine,
            meta.Stage,
            meta.Title,
            new Dictionary<Difficulty, OptionDifficultySnapshot>
            {
                [Difficulty.Master] = new(Difficulty.Master, 4321, new UmgrChart(), meta)
            });
        using var resourceStore = new DummyResourceStore(workPath);
        var context = new MusicExportContext(
            TestAssets.Load(),
            TestMediaTool.Instance,
            resourceStore,
            DummyInfrastructureAssetProvider.Instance);

        try
        {
            var result = await OptionExporter.ExportAsync(context, settings, outputPaths, [book], workPath,
                CancellationToken.None);

            Assert.True(result.Succeeded);
            var musicXmlPath = Path.Combine(outputPaths.MusicFolder, "music4321", "Music.xml");
            var releaseTagName = XDocument.Load(musicXmlPath).Root?.Element("releaseTagName");
            Assert.NotNull(releaseTagName);
            Assert.Equal("123", releaseTagName.Element("id")?.Value);
            Assert.Equal("My Pack", releaseTagName.Element("str")?.Value);
            Assert.Equal(string.Empty, releaseTagName.Element("data")?.Value);
            Assert.False(Directory.Exists(outputPaths.ReleaseTagPath));
        }
        finally
        {
            if (Directory.Exists(workPath)) Directory.Delete(workPath, true);
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

    private sealed class DummyInfrastructureAssetProvider : IInfrastructureAssetProvider
    {
        public static readonly DummyInfrastructureAssetProvider Instance = new();

        public string GetPath(InfrastructureAsset asset)
        {
            return string.Empty;
        }
    }
}