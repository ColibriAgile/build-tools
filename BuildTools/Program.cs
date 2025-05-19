using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Abstractions;
using Spectre.Console;
using System.Diagnostics;
using BuildTools.Commands;
using BuildTools.Services;

var builder = Host.CreateApplicationBuilder();
var services = builder.Services;
var console = AnsiConsole.Console;
services.AddSingleton(console);
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<FileListingService>();
services.AddSingleton<ManifestoService>();
services.AddSingleton<EmpacotarCommand>();
services.AddSingleton<IZipService, ZipService>();

using var host = builder.Build();
await host.StartAsync().ConfigureAwait(false);

var rootCommand = new RootCommand("EmpacotarNet9 - Empacotador de arquivos estilo Python");
rootCommand.AddCommand(host.Services.GetRequiredService<EmpacotarCommand>());

await rootCommand.InvokeAsync(args).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}

