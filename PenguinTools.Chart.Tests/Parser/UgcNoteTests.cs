using PenguinTools.Chart.Models;
using PenguinTools.Chart.Parser.ugc;
using Xunit;

namespace PenguinTools.Chart.Tests.Parser;

public class UgcNoteTests
{
    private const string Header =
        "@VER\t8\n@TICKS\t480\n@BPM\t0'0\t120.0\n@BEAT\t0\t4\t4\n";

    private static async Task<Models.umgr.Chart> Parse(string body)
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
        var tap = Assert.Single(chart.Notes.Children.OfType<Models.umgr.Tap>());
        Assert.Equal(480, tap.Tick.Original);
        Assert.Equal(6, tap.Lane);
        Assert.Equal(4, tap.Width);
    }

    [Fact]
    public async Task ExTap_WithEffect()
    {
        var chart = await Parse("#0'480:x64U\n");
        var ex = Assert.Single(chart.Notes.Children.OfType<Models.umgr.ExTap>());
        Assert.Equal(ExEffect.UP, ex.Effect);
    }

    [Fact]
    public async Task Flick_And_Damage()
    {
        var chart = await Parse("#0'480:f24\n#0'960:d24\n");
        Assert.Single(chart.Notes.Children.OfType<Models.umgr.Flick>());
        Assert.Single(chart.Notes.Children.OfType<Models.umgr.Damage>());
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
        var hold = Assert.Single(chart.Notes.Children.OfType<Models.umgr.Hold>());
        Assert.Equal(0, hold.Tick.Original);
        var end = Assert.Single(hold.Children.OfType<Models.umgr.HoldJoint>());
        Assert.Equal(480, end.Tick.Original);
    }

    [Fact]
    public async Task Slide_WithControlAndEnd()
    {
        var chart = await Parse("#0'0:s14\n#240>s24C\n#480>s34D\n");
        var slide = Assert.Single(chart.Notes.Children.OfType<Models.umgr.Slide>());
        Assert.Equal(2, slide.Children.Count);
        var c = Assert.IsType<Models.umgr.SlideJoint>(slide.Children[0]);
        var e = Assert.IsType<Models.umgr.SlideJoint>(slide.Children[1]);
        Assert.Equal(Joint.C, c.Joint);
        Assert.Equal(Joint.D, e.Joint);
    }

    [Fact]
    public async Task Air_PairsWithPrecedingTap()
    {
        var chart = await Parse("#0'480:t64\n#0'480:a64UN\n");
        var tap = Assert.Single(chart.Notes.Children.OfType<Models.umgr.Tap>());
        var air = Assert.Single(chart.Notes.Children.OfType<Models.umgr.Air>());
        Assert.Same(tap, air.PairNote);
        Assert.Equal(AirDirection.IR, air.Direction);
        Assert.Equal(Color.DEF, air.Color);
    }

    [Fact]
    public async Task AirHold_ParentAndChild()
    {
        var chart = await Parse("#0'0:t14\n#0'0:H140\n#480>H143\n");
        var sl = Assert.Single(chart.Notes.Children.OfType<Models.umgr.AirSlide>());
        Assert.Single(sl.Children.OfType<Models.umgr.AirSlideJoint>());
    }

    [Fact]
    public async Task AirCrush_WithIntervalAndColor()
    {
        var chart = await Parse("#0'0:C1410,100\n");
        var crash = Assert.Single(chart.Notes.Children.OfType<Models.umgr.AirCrash>());
        Assert.Equal(Color.RED, crash.Color);
        Assert.Equal(100, crash.Density.Original);
    }
}
