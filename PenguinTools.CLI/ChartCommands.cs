using System.CommandLine;
using PenguinTools.Chart.Converter.c2s;
using PenguinTools.Chart.Writer.c2s;
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
            Description = "Path to the source chart (.mgxc, .ugc, or .sus)."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Path to the output .c2s file."
        };

        var command = new Command("convert", "Convert an MGXC, UGC, or SUS chart into a C2S chart file.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("chart convert", outputOptions, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(input, output));

                var converted = new C2SChartConverter(new C2SConvertRequest(parsed.Value)).Convert();
                if (!converted.Succeeded || converted.Value is null)
                {
                    var failed = OperationResult.Failure()
                        .WithDiagnostics(parsed.Diagnostics.Merge(converted.Diagnostics));
                    return new CliCommandOutcome(failed, Data: new CliCommandData(input, output));
                }

                CliPaths.EnsureParentDirectory(output);
                var written = await new C2SChartWriter(new C2SWriteRequest(output, converted.Value)).WriteAsync(ct);
                var result = CliPaths.Merge(parsed.Diagnostics.Merge(converted.Diagnostics), written);
                var data = CliOperations.CreateChartConvertData(input, output, parsed.Value.Meta);
                var message = result.Succeeded ? $"Wrote chart: {output}" : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }
}
