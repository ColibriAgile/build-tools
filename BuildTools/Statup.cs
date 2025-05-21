using System.IO.Abstractions;
using BuildTools.Commands;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BuildTools;

using System.CommandLine;

/// <summary>
/// Configuração de serviços para o projeto.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Configura os serviços necessários para o projeto.
    /// </summary>
    /// <param name="services">Coleção de serviços a serem configurados.</param>
    /// <returns>A coleção de serviços configurada.</returns>
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
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

        services.AddKeyedSingleton("silencioso", silenciosoOption);
        services.AddKeyedSingleton("semCor", semCorOption);
        services.AddKeyedSingleton("resumo", resumoOption);

        return services;
    }

    /// <summary>
    /// Configura todos os comandos para a aplicação.
    /// </summary>
    /// <param name="serviceProvider">
    /// O provedor de serviços a ser utilizado para resolver dependências.
    /// </param>
    /// <returns>
    /// O comando raiz configurado com todos os comandos e opções globais.
    /// </returns>
    public static RootCommand ConfigureCommands(this IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("Colibri BuildTools - Empacotador de soluções");

        var silenciosoOption = serviceProvider.GetRequiredKeyedService<Option<bool>>("silencioso");
        var semCorOption = serviceProvider.GetRequiredKeyedService<Option<bool>>("semCor");
        var resumoOption = serviceProvider.GetRequiredKeyedService<Option<bool>>("resumo");

        rootCommand.AddGlobalOption(silenciosoOption);
        rootCommand.AddGlobalOption(semCorOption);
        rootCommand.AddGlobalOption(resumoOption);

        var empacotarCommand = serviceProvider.GetRequiredService<EmpacotarCommand>();
        var empacotarScriptsCommand = serviceProvider.GetRequiredService<EmpacotarScriptsCommand>();

        rootCommand.Add(empacotarCommand);
        rootCommand.Add(empacotarScriptsCommand);

        return rootCommand;
    }
}
