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
    [InlineData('L', PenguinTools.Chart.Models.ExEffect.LS)]
    [InlineData('R', PenguinTools.Chart.Models.ExEffect.RS)]
    [InlineData('A', PenguinTools.Chart.Models.ExEffect.LC)]   // rotate-left anticlockwise
    [InlineData('W', PenguinTools.Chart.Models.ExEffect.RC)]   // rotate-right
    [InlineData('I', PenguinTools.Chart.Models.ExEffect.BS)]   // in-out burst
    public void ExEffectChar_MapsToExpectedEffect(char c, PenguinTools.Chart.Models.ExEffect expected)
    {
        Assert.Equal(expected, UgcPayload.ExEffectChar(c));
    }
}
