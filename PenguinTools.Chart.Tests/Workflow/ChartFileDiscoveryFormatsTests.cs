using System.Text.Json;
using PenguinTools.Workflow;
using Xunit;

namespace PenguinTools.Chart.Tests.Workflow;

public class ChartFileDiscoveryFormatsTests
{
    [Fact]
    public void TryParse_AcceptsBracketedOrderedList()
    {
        var ok = ChartFileDiscoveryFormats.TryParse("[ugc, mgxc]", out var formats, out var error);

        Assert.True(ok, error);
        Assert.Equal([ChartFileFormat.Ugc, ChartFileFormat.Mgxc], formats);
    }

    [Fact]
    public void TryParse_RemovesDuplicates_PreservingFirstOccurrence()
    {
        var ok = ChartFileDiscoveryFormats.TryParse("ugc, ugc, mgxc", out var formats, out var error);

        Assert.True(ok, error);
        Assert.Equal([ChartFileFormat.Ugc, ChartFileFormat.Mgxc], formats);
    }

    [Fact]
    public void OptionDocumentJson_ReadsNewArraySyntax()
    {
        const string json = """
                            {
                              "optionName": "TEST",
                              "chartFileDiscovery": ["ugc", "mgxc"]
                            }
                            """;

        var document = JsonSerializer.Deserialize<OptionDocument>(json, OptionDocumentJson.Default);

        Assert.NotNull(document);
        Assert.Equal([ChartFileFormat.Ugc, ChartFileFormat.Mgxc], document.ChartFileDiscovery);
    }

    [Fact]
    public void OptionDocumentJson_RejectsLegacyNumericMode()
    {
        const string json = """
                            {
                              "optionName": "TEST",
                              "chartFileDiscovery": 2
                            }
                            """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OptionDocument>(json, OptionDocumentJson.Default));
    }

    [Fact]
    public void OptionDocumentJson_RejectsLegacyEnumName()
    {
        const string json = """
                            {
                              "optionName": "TEST",
                              "chartFileDiscovery": "ugcFirst"
                            }
                            """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OptionDocument>(json, OptionDocumentJson.Default));
    }

    [Fact]
    public void OptionDocumentJson_WritesOrderedArraySyntax()
    {
        var document = new OptionDocument
        {
            OptionName = "TEST",
            ChartFileDiscovery = [ChartFileFormat.Ugc, ChartFileFormat.Mgxc]
        };

        var json = JsonSerializer.Serialize(document, OptionDocumentJson.Default);

        Assert.Contains("\"chartFileDiscovery\": [", json);
        Assert.Contains("\"ugc\"", json);
        Assert.Contains("\"mgxc\"", json);
    }
}
