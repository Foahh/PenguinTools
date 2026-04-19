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
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("chart convert", outputFormat, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(InputPath: input, OutputPath: output, AssetRoot: assetRoot));
                }

                CliPaths.EnsureParentDirectory(output);
                var written = await new C2SChartWriter(new C2SWriteRequest(output, parsed.Value)).WriteAsync(ct);
                var result = CliPaths.Merge(parsed.Diagnostics, written);
                var data = CliOperations.CreateChartConvertData(input, output, assetRoot, parsed.Value.Meta);
                var message = result.Succeeded ? $"Wrote chart: {output}" : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }
}
