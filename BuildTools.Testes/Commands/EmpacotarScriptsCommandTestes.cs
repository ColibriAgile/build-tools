using BuildTools.Commands;
using BuildTools.Models;
using BuildTools.Services;
using Spectre.Console.Testing;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes.Commands;

using Spectre.Console;

[ExcludeFromCodeCoverage]
public sealed class EmpacotarScriptsCommandTestes
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly TestConsole _console = new();
    private readonly IEmpacotadorScriptsService _service;
    private readonly Option<bool> _silenciosoOption = new(aliases: ["--silencioso", "-s"]);
    private readonly Option<bool> _semCorOption = new(aliases: ["--sem-cor", "-sc"]);
    private readonly Option<string> _resumoOption = new(aliases: ["--resumo", "-r"]);
    private readonly RootCommand _rootCommand = new("Colibri BuildTools - Empacotador de soluções");

    public EmpacotarScriptsCommandTestes()
    {
        _service = Substitute.For<IEmpacotadorScriptsService>();
        var cmd = new EmpacotarScriptsCommand(_silenciosoOption, _semCorOption, _resumoOption, _fileSystem, _console, _service);
        _rootCommand.AddGlobalOption(_resumoOption);
        _rootCommand.AddGlobalOption(_silenciosoOption);
        _rootCommand.AddGlobalOption(_semCorOption);
        _rootCommand.AddCommand(cmd);
    }

    [Fact]
    public async Task InvokeAsync_QuandoPastaNaoExiste_DeveExibirErro()
    {
        // Arrange
        const string PASTA = @"C:\naoexiste";
        _fileSystem.Directory.Exists(PASTA).Returns(false);

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA, "--saida", @"C:\saida"]);

        // Assert
        result.ShouldNotBe(0); // Deve retornar erro
        _console.Output.ShouldContain("ERROR");
    }

    [Fact]
    public async Task InvokeAsync_QuandoProcessarEmpacotamentoLancaExcecao_DeveExibirErro()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, false).Returns(static _ => throw new Exception("Falha interna"));

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldNotBe(0);
        _console.Output.ShouldContain("ERROR");
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoTrue_DeveExibirResumoMarkdown()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";

        var resultado = new EmpacotamentoScriptResultado
        (
            ["arquivo1.sql", "arquivo2.sql"],
            [("_scripts01.zip", "scripts.zip")]
        );

        _service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, false).Returns(resultado);
        _fileSystem.Directory.GetCurrentDirectory().Returns(PASTA_ORIGEM);

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA, "--resumo", "markdown"]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("## Resumo dos pacotes gerados");
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoTrue_DeveExibirResumoConsole()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";

        var resultado = new EmpacotamentoScriptResultado
        (
            ["_01scriptsSqlServer.zip", "scriptsPgSql.zip"],
            [("_01scriptsSqlServer.zip", "scriptsSqlServer.zip")]
        );

        _service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, false).Returns(resultado);
        _fileSystem.Directory.GetCurrentDirectory().Returns(PASTA_ORIGEM);

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA, "--resumo", "console"]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("Resumo dos pacotes gerados");
        _console.Output.ShouldContain("scriptsPgSql.zip");
        _console.Output.ShouldContain("scriptsSqlServer.zip");
    }

    [Fact]
    public async Task InvokeAsync_QuandoSilenciosoTrue_NaoDeveExibirMensagens()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";

        var resultado = new EmpacotamentoScriptResultado
        (
            ["_01scriptsSqlServer.zip", "scriptsPgSql.zip"],
            [("_01scriptsSqlServer.zip", "scriptsSqlServer.zip")]
        );

        _service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, true).Returns(resultado);
        _fileSystem.Directory.GetCurrentDirectory().Returns(PASTA_ORIGEM);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "empacotar_scripts",
                "--pasta",
                PASTA_ORIGEM,
                "--saida",
                PASTA_SAIDA,
                "--silencioso",
                "true"
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldNotContain("INFO");
        _console.Output.ShouldNotContain("SUCCESS");
    }

    [Fact]
    public async Task InvokeAsync_QuandoSemCorTrue_DeveDesabilitarAnsiConsole()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";

        var resultado = new EmpacotamentoScriptResultado
        (
            ["_01scriptsSqlServer.zip", "scriptsPgSql.zip"],
            [("_01scriptsSqlServer.zip", "scriptsSqlServer.zip")]
        );

        _service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, false).Returns(resultado);
        _fileSystem.Directory.GetCurrentDirectory().Returns(PASTA_ORIGEM);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "empacotar_scripts",
                "--pasta",
                PASTA_ORIGEM,
                "--saida",
                PASTA_SAIDA,
                "--sem-cor",
                "true"
            ]
        );

        // Assert
        result.ShouldBe(0);
        AnsiConsole.Profile.Capabilities.Ansi.ShouldBeFalse();
    }
}
