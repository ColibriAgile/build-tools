using System.CommandLine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BuildTools;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("BuildTools.Testes")]

var rootCommand = new ServiceCollection()
    .ConfigureServices()
    .BuildServiceProvider()
    .ConfigureCommands();

await rootCommand.InvokeAsync(args, new AnsiConsoleWrapper()).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Digite qualquer tecla para sair...");
    Console.Read();
}

