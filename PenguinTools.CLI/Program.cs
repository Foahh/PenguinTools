using System.CommandLine;
using PenguinTools.CLI;

var rootCommand = RootCommands.BuildRootCommand();
var parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0 && RootCommands.GetOutputFormat(parseResult) == CliOutputFormat.Json)
{
    CliOutput.Write("parse", CliOutputFormat.Json, CliOperations.CreateParseErrorOutcome(parseResult));
    return 1;
}

return await parseResult.InvokeAsync();
