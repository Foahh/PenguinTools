using System.Text;
using PenguinTools.CLI;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var rootCommand = RootCommands.BuildRootCommand();
var parseResult = rootCommand.Parse(args);
var outputOptions = RootCommands.GetOutputOptions(parseResult);

if (parseResult.Errors.Count > 0 && outputOptions.Format == CliOutputFormat.Json)
{
    CliOutput.Write("parse", outputOptions, CliOperations.CreateParseErrorOutcome(parseResult));
    return 1;
}

return await parseResult.InvokeAsync();
