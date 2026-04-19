using PenguinTools.Chart.Parser.ugc;
using Xunit;

namespace PenguinTools.Chart.Tests.Parser;

public class PayloadTests
{
    [Theory]
    [InlineData('0', 0)]
    [InlineData('9', 9)]
    [InlineData('a', 10)]
    [InlineData('z', 35)]
    [InlineData('A', 10)]  // uppercase also accepted
    [InlineData('Z', 35)]
    public void Base36_ValidChars_MapsCorrectly(char c, int expected)
    {
        Assert.Equal(expected, UgcPayload.Base36(c));
    }

    [Theory]
    [InlineData('!')]
    [InlineData(' ')]
    public void Base36_InvalidChars_ReturnsMinusOne(char c)
    {
        Assert.Equal(-1, UgcPayload.Base36(c));
    }

    [Theory]
    [InlineData('U', PenguinTools.Chart.Models.ExEffect.UP)]
    [InlineData('D', PenguinTools.Chart.Models.ExEffect.DW)]
    [InlineData('C', PenguinTools.Chart.Models.ExEffect.CE)]
    [InlineData('L', PenguinTools.Chart.Models.ExEffect.RS)]
    [InlineData('R', PenguinTools.Chart.Models.ExEffect.LS)]
    [InlineData('A', PenguinTools.Chart.Models.ExEffect.RC)]
    [InlineData('W', PenguinTools.Chart.Models.ExEffect.LC)]
    [InlineData('I', PenguinTools.Chart.Models.ExEffect.BS)]   // in-out burst
    public void ExEffectChar_MapsToExpectedEffect(char c, PenguinTools.Chart.Models.ExEffect expected)
    {
        Assert.Equal(expected, UgcPayload.ExEffectChar(c));
    }

    [Theory]
    [InlineData("UC", PenguinTools.Chart.Models.AirDirection.IR)]
    [InlineData("UL", PenguinTools.Chart.Models.AirDirection.UR)]
    [InlineData("UR", PenguinTools.Chart.Models.AirDirection.UL)]
    [InlineData("DC", PenguinTools.Chart.Models.AirDirection.DW)]
    [InlineData("DL", PenguinTools.Chart.Models.AirDirection.DR)]
    [InlineData("DR", PenguinTools.Chart.Models.AirDirection.DL)]
    public void AirDirectionCode_MapsToExpectedDirection(string code, PenguinTools.Chart.Models.AirDirection expected)
    {
        Assert.Equal(expected, UgcPayload.AirDirectionCode(code));
    }

    [Theory]
    [InlineData('N', PenguinTools.Chart.Models.Color.DEF)]
    [InlineData('I', PenguinTools.Chart.Models.Color.PNK)]
    public void AirColorChar_MapsToExpectedColor(char c, PenguinTools.Chart.Models.Color expected)
    {
        Assert.Equal(expected, UgcPayload.AirColorChar(c));
    }

    [Theory]
    [InlineData("00", 0)]
    [InlineData("0A", 10)]
    [InlineData("10", 36)]
    public void Height36_ParsesTwoDigitBase36(string s, decimal expected)
    {
        Assert.Equal(expected, UgcPayload.Height36(s));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("24", 24)]
    [InlineData("$", 0x7FFFFFFF)]
    public void AirCrashInterval_MapsExpectedDensity(string text, int expected)
    {
        Assert.Equal(expected, UgcPayload.AirCrashInterval(text));
    }
}
