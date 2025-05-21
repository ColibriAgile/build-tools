using System.CommandLine;
using System.Diagnostics;
using BuildTools;
using Microsoft.Extensions.DependencyInjection;

var rootCommand = new ServiceCollection()
    .ConfigureServices()
    .BuildServiceProvider()
    .ConfigureCommands();

await rootCommand.InvokeAsync(args).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Digite qualquer tecla para sair...");
    Console.Read();
}

