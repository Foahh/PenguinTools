using PenguinTools.Core;
using Xunit;

namespace PenguinTools.Chart.Tests.Parser;

public class DiagnosticLocationTests
{
    [Fact]
    public void FormattedLocation_UsesLineNumbers_ForTextFiles()
    {
        var diagnostic = new Diagnostic(Severity.Warning, "msg", @"D:\charts\test.ugc", line: 12);

        Assert.Equal(@"D:\charts\test.ugc(12)", diagnostic.FormattedLocation);
    }

    [Fact]
    public void FormattedLocation_UsesHexOffsets_ForMgxcFiles()
    {
        var diagnostic = new Diagnostic(Severity.Warning, "msg", @"D:\charts\test.mgxc", line: 26);

        Assert.Equal(@"D:\charts\test.mgxc(0x1A)", diagnostic.FormattedLocation);
    }
}
