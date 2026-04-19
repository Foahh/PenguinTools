using PenguinTools.Chart.Models;
using PenguinTools.Chart.Parser;
using PenguinTools.Core;
using Xunit;

namespace PenguinTools.Chart.Tests.Parser;

public class UgcNoteTests
{
    private const string Header =
        "@VER\t8\n@TICKS\t480\n@BPM\t0'0\t120.0\n@BEAT\t0\t4\t4\n";

    private static async Task<Models.mgxc.Chart> Parse(string body)
    {
        var tmp = Path.GetTempFileName() + ".ugc";
        await File.WriteAllTextAsync(tmp, Header + body);
        try
        {
            var r = await new UgcParser(new UgcParseRequest(tmp, TestAssets.Load()), TestMediaTool.Instance).ParseAsync();
            Assert.True(r.Succeeded, r.ToString());
            return r.Value!;
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Tap_Simple()
    {
        var chart = await Parse("#0'480:t64\n");
        var tap = Assert.Single(chart.Notes.Children.OfType<Models.mgxc.Tap>());
        Assert.Equal(480, tap.Tick.Original);
        Assert.Equal(6, tap.Lane);
        Assert.Equal(4, tap.Width);
    }

    [Fact]
    public async Task ExTap_WithEffect()
    {
        var chart = await Parse("#0'480:x64U\n");
        var ex = Assert.Single(chart.Notes.Children.OfType<Models.mgxc.ExTap>());
        Assert.Equal(ExEffect.UP, ex.Effect);
    }

    [Fact]
    public async Task Flick_And_Damage()
    {
        var chart = await Parse("#0'480:f24\n#0'960:d24\n");
        Assert.Single(chart.Notes.Children.OfType<Models.mgxc.Flick>());
        Assert.Single(chart.Notes.Children.OfType<Models.mgxc.Damage>());
    }

    [Fact]
    public async Task Click_IsSkipped()
    {
        var chart = await Parse("#0'480:c24\n");
        Assert.Empty(chart.Notes.Children);
    }

    [Fact]
    public async Task Hold_ParentAndEnd()
    {
        var chart = await Parse("#0'0:h64\n#480>s64\n");
        var hold = Assert.Single(chart.Notes.Children.OfType<Models.mgxc.Hold>());
        Assert.Equal(0, hold.Tick.Original);
        var end = Assert.Single(hold.Children.OfType<Models.mgxc.HoldJoint>());
        Assert.Equal(480, end.Tick.Original);
    }
}
