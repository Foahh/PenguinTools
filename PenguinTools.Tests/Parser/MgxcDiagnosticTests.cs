using PenguinTools.Chart.Parser.mgxc;
using PenguinTools.Core.Diagnostic;
using Xunit;

namespace PenguinTools.Tests.Parser;

public class MgxcDiagnosticTests
{
    [Fact]
    public async Task InvalidHeader_DiagnosticIncludesFileAndOffset()
    {
        var tmp = Path.GetTempFileName() + ".mgxc";
        try
        {
            await File.WriteAllBytesAsync(tmp, "NOPE"u8.ToArray());

            var result = await new MgxcParser(
                new MgxcParseRequest(tmp, TestAssets.Load()),
                TestMediaTool.Instance).ParseAsync();

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics.Diagnostics);
            Assert.Equal(Severity.Error, diagnostic.Severity);
            Assert.Equal(tmp, diagnostic.Path);
            Assert.Equal(0, diagnostic.Line);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}