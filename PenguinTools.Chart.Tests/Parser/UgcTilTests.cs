using PenguinTools.Chart.Parser;
using PenguinTools.Core;
using Xunit;

namespace PenguinTools.Chart.Tests.Parser;

public class UgcTilTests
{
    [Fact]
    public async Task Til_Definition_CreatesScrollSpeedEvent()
    {
        const string ugc =
            "@VER\t8\n@TICKS\t480\n" +
            "@BPM\t0'0\t120.0\n@BEAT\t0\t4\t4\n" +
            "@TIL\t3\t0'240\t10000.0\n";
        var tmp = Path.GetTempFileName() + ".ugc";
        await File.WriteAllTextAsync(tmp, ugc);
        try
        {
            var r = await new UgcParser(new UgcParseRequest(tmp, TestAssets.Load()), TestMediaTool.Instance).ParseAsync();
            Assert.True(r.Succeeded);
            var sse = r.Value!.Events.Children
                .OfType<PenguinTools.Chart.Models.mgxc.ScrollSpeedEvent>()
                .SingleOrDefault(e => e.Speed == 10000.0m);
            Assert.NotNull(sse);
            Assert.Equal(240, sse!.Tick.Original);
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task MainTil_SetsMetaMainTil()
    {
        const string ugc =
            "@VER\t8\n@TICKS\t480\n" +
            "@BPM\t0'0\t120.0\n@BEAT\t0\t4\t4\n" +
            "@MAINTIL\t2\n";
        var tmp = Path.GetTempFileName() + ".ugc";
        await File.WriteAllTextAsync(tmp, ugc);
        try
        {
            var r = await new UgcParser(new UgcParseRequest(tmp, TestAssets.Load()), TestMediaTool.Instance).ParseAsync();
            Assert.True(r.Succeeded);
            var errors = r.Diagnostics.Diagnostics.Where(d => d.Severity >= Severity.Warning).ToList();
            Assert.DoesNotContain(errors, d => d.Message.Contains("MAINTIL"));
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
