using BuildTools.Commands;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes;

[ExcludeFromCodeCoverage]
public sealed class StartupTestes
{
    [Fact]
    public void ConfigureServices_DeveRegistrarServicosEsperados()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.ConfigureServices();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IFileSystem>().ShouldNotBeNull();
        provider.GetService<IEmpacotadorService>().ShouldNotBeNull();
        provider.GetService<EmpacotarCommand>().ShouldNotBeNull();
        provider.GetService<EmpacotarScriptsCommand>().ShouldNotBeNull();
        provider.GetService<IEmpacotadorScriptsService>().ShouldNotBeNull();
        provider.GetService<IZipService>().ShouldNotBeNull();
        provider.GetService<IManifestoService>().ShouldNotBeNull();
        provider.GetService<IArquivoListagemService>().ShouldNotBeNull();
        provider.GetService<IArquivoService>().ShouldNotBeNull();
        provider.GetService<IAnsiConsole>().ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureRootCommand_DeveAdicionarOpcoesGlobais()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .ConfigureServices()
            .BuildServiceProvider();

        // Act
        var rootCommand = serviceProvider.ConfigureCommands();

        // Assert
        rootCommand.ShouldNotBeNull();
        rootCommand.Options.ShouldContain(static o => o.HasAlias("--silencioso"));
        rootCommand.Options.ShouldContain(static o => o.HasAlias("--sem-cor"));
        rootCommand.Options.ShouldContain(static o => o.HasAlias("--resumo"));
        rootCommand.Children.ShouldContain(static c => c is EmpacotarCommand);
        rootCommand.Children.ShouldContain(static c => c is EmpacotarScriptsCommand);
    }
}
