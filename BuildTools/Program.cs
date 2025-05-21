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
services.AddSingleton<IEmpacotadorScriptsService, EmpacotadorScriptsService>();
services.AddSingleton<IZipService, ZipService>();
services.AddSingleton<IManifestoService, ManifestoService>();
services.AddSingleton<IArquivoListagemService, ArquivoListagemService>();
services.AddSingleton<IArquivoService, ArquivoService>();

var rootCommand = new RootCommand("Colibri BuildTools - Empacotador de soluções");

var silenciosoOption = new Option<bool>
(
    aliases: ["--silencioso", "-sl"],
    description: "Executa o comando de forma silenciosa, exibindo apenas erros. (global)"
);

var semCorOption = new Option<bool>
(
    aliases: ["--sem-cor", "-sc"],
    description: "Desabilita cores ANSI na saída. (global)"
);

var resumoOption = new Option<bool>
(
    aliases: ["--resumo", "-r"],
    description: "Exibe um resumo em Markdown ao final. (global)"
);

rootCommand.AddGlobalOption(silenciosoOption);
rootCommand.AddGlobalOption(semCorOption);
rootCommand.AddGlobalOption(resumoOption);

services.AddKeyedSingleton("silencioso", silenciosoOption);
services.AddKeyedSingleton("semCor", semCorOption);
services.AddKeyedSingleton("resumo", resumoOption);

var serviceProvider = services.BuildServiceProvider();

rootCommand.AddCommand(serviceProvider.GetRequiredService<EmpacotarCommand>());
rootCommand.AddCommand(serviceProvider.GetRequiredService<EmpacotarScriptsCommand>());

await rootCommand.InvokeAsync(args).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Digite qualquer tecla para sair...");
    Console.Read();
}

