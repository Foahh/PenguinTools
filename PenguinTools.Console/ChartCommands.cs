using System.CommandLine;
using PenguinTools.Chart.Writer;
using PenguinTools.Core;

namespace PenguinTools.CLI;

internal static class ChartCommands
{
    internal static Command BuildChartCommand()
    {
        var command = new Command("chart", "Chart parsing and conversion commands.");
        command.Subcommands.Add(BuildChartConvertCommand());
        return command;
    }

    private static Command BuildChartConvertCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to the source .mgxc chart."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Path to the output .c2s file."
        };
        var assetRootOption = new Option<string?>("--asset-root")
        {
            Description = "Optional directory to scan for additional asset XML before parsing."
        };

        var command = new Command("convert", "Convert an MGXC chart into a C2S chart file.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(assetRootOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var assetRoot = CliPaths.ResolveOptionalPath(parseResult.GetValue(assetRootOption));

            return await CliOperations.ExecuteAsync(async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return parsed.ToResult();
                }

                CliPaths.EnsureParentDirectory(output);
                var written = await new C2SChartWriter(new C2SWriteRequest(output, parsed.Value)).WriteAsync(ct);
                var result = CliPaths.Merge(parsed.Diagnostics, written);

                if (result.Succeeded)
                {
                    Console.WriteLine($"Wrote chart: {output}");
                }

                return result;
            }, cancellationToken);
        });

        return command;
    }
}
