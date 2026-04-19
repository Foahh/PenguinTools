using PenguinTools.Chart.Parser;
using PenguinTools.Core;
using PenguinTools.Core.Metadata;
using Xunit;

namespace PenguinTools.Chart.Tests.Parser;

public class UgcMetaTests
{
    private static async Task<OperationResult<PenguinTools.Chart.Models.umgr.Chart>> Parse(string ugc)
    {
        var tmp = Path.GetTempFileName() + ".ugc";
        await File.WriteAllTextAsync(tmp, ugc);
        try
        {
            var parser = new UgcParser(
                new UgcParseRequest(tmp, TestAssets.Load()),
                TestMediaTool.Instance);
            return await parser.ParseAsync();
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Meta_BasicFields_PopulatedFromHeader()
    {
        const string ugc =
            "@VER\t8\n" +
            "@TICKS\t480\n" +
            "@TITLE\tHello World\n" +
            "@ARTIST\tAlice\n" +
            "@DESIGN\tBob\n" +
            "@DIFF\t3\n" +
            "@CONST\t13.50\n" +
            "@SONGID\t5097\n" +
            "@BPM\t0'0\t200.0\n" +
            "@BEAT\t0\t4\t4\n";

        var r = await Parse(ugc);
        Assert.True(r.Succeeded, r.ToString());
        var m = r.Value!.Meta;

        Assert.Equal("Hello World", m.Title);
        Assert.Equal("Alice", m.Artist);
        Assert.Equal("Bob", m.Designer);
        Assert.Equal(Difficulty.Master, m.Difficulty);
        Assert.Equal(13.50m, m.Level);
        Assert.Equal("5097", m.MgxcId);
        Assert.Equal(5097, m.Id);
    }

    [Fact]
    public async Task Meta_WrongVersion_Fails()
    {
        const string ugc = "@VER\t7\n@TICKS\t480\n@BPM\t0'0\t120.0\n";
        var r = await Parse(ugc);
        Assert.False(r.Succeeded);
    }

    [Fact]
    public async Task Meta_WrongTicks_Fails()
    {
        const string ugc = "@VER\t8\n@TICKS\t960\n@BPM\t0'0\t120.0\n";
        var r = await Parse(ugc);
        Assert.False(r.Succeeded);
    }

    [Fact]
    public async Task Meta_FlagSoffset_SetsBarOffset()
    {
        const string ugc =
            "@VER\t8\n" +
            "@TICKS\t480\n" +
            "@FLAG\tSOFFSET\tTRUE\n" +
            "@BPM\t0'0\t120.0\n" +
            "@BEAT\t0\t4\t4\n";
        var r = await Parse(ugc);
        Assert.True(r.Succeeded);
        Assert.True(r.Value!.Meta.BgmEnableBarOffset);
    }
}
