using System.CommandLine;
using PenguinTools.CLI;

var rootCommand = RootCommands.BuildRootCommand();
return await rootCommand.Parse(args).InvokeAsync();
