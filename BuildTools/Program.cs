using System.CommandLine;
using System.Diagnostics;
using System.IO.Abstractions;
using BuildTools.Commands;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

var services = new ServiceCollection();
    
var console = AnsiConsole.Console;
services.AddSingleton(console);
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<IEmpacotadorService, EmpacotadorService>();
services.AddSingleton<EmpacotarCommand>();
services.AddSingleton<EmpacotarScriptsCommand>();
services.AddSingleton<EmpacotadorScriptsService>();
services.AddSingleton<IZipService, ZipService>();
services.AddSingleton<IManifestoService, ManifestoService>();
services.AddSingleton<IArquivoListagemService, ArquivoListagemService>();
services.AddSingleton<IArquivoService, ArquivoService>();
services.AddSingleton<IVersaoBaseService, VersaoBaseService>();

var serviceProvider = services.BuildServiceProvider();

var rootCommand = new RootCommand("Colibri BuildTools - Empacotador de soluções");
rootCommand.AddCommand(serviceProvider.GetRequiredService<EmpacotarCommand>());
rootCommand.AddCommand(serviceProvider.GetRequiredService<EmpacotarScriptsCommand>());

await rootCommand.InvokeAsync(args).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}

