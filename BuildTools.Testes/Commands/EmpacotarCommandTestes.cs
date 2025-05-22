using BuildTools.Commands;
using BuildTools.Models;
using BuildTools.Services;
using Spectre.Console.Testing;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes.Commands;

[ExcludeFromCodeCoverage]
public sealed class EmpacotarCommandTestes
{
    private readonly IEmpacotadorService _empacotadorService = Substitute.For<IEmpacotadorService>();
    private readonly TestConsole _console = new();
    private readonly Option<bool> _silenciosoOption = new(aliases: ["--silencioso", "-s"]);
    private readonly Option<bool> _semCorOption = new(aliases: ["--sem-cor", "-sc"]);
    private readonly Option<string> _resumoOption = new(aliases: ["--resumo", "-r"]);
    private readonly RootCommand _rootCommand = new("Colibri BuildTools - Empacotador de soluções");

    public EmpacotarCommandTestes()
    {
        var cmd = new EmpacotarCommand(_silenciosoOption, _semCorOption, _resumoOption, _empacotadorService, _console);
        _rootCommand.AddGlobalOption(_resumoOption);
        _rootCommand.AddGlobalOption(_silenciosoOption);
        _rootCommand.AddGlobalOption(_semCorOption);
        _rootCommand.AddCommand(cmd);
    }

    [Fact]
    public async Task InvokeAsync_QuandoEmpacotamentoComSucesso_DeveExibirMensagemDeSucesso()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string SAIDA = @"C:\saida";
        const string CAMINHO_PACOTE = @"C:\saida\pacote.zip";

        _empacotadorService.Empacotar(PASTA, SAIDA, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(new EmpacotamentoResultado(CAMINHO_PACOTE, ["manifesto.dat", "arquivo1.txt"]));

        // Act
        var result = await _rootCommand.InvokeAsync
        ([
            "empacotar",
            "--pasta", PASTA,
            "--saida", SAIDA
        ]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("SUCCESS");
        _console.Output.ShouldContain(CAMINHO_PACOTE);
    }

    [Fact]
    public async Task InvokeAsync_QuandoEmpacotamentoFalha_DeveExibirMensagemDeErro()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string SAIDA = @"C:\saida";

        _empacotadorService.Empacotar(PASTA, SAIDA, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(static _ => throw new Exception("Falha ao empacotar"));

        // Act
        var result = await _rootCommand.InvokeAsync
        ([
            "empacotar",
            "--pasta", PASTA,
            "--saida", SAIDA
        ]);

        // Assert
        result.ShouldNotBe(0);
        _console.Output.ShouldContain("ERROR");
        _console.Output.ShouldContain("Falha ao empacotar");
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoTrue_DeveExibirResumoMarkdown()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string SAIDA = @"C:\saida";
        const string CAMINHO_PACOTE = @"C:\saida\pacote.zip";

        _empacotadorService.Empacotar(PASTA, SAIDA, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(new EmpacotamentoResultado(CAMINHO_PACOTE, ["manifesto.dat", "arquivo1.txt"]));

        // Act
        var result = await _rootCommand.InvokeAsync
        ([
            "empacotar",
            "--pasta", PASTA,
            "--saida", SAIDA,
            "--resumo", "markdown"
        ]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("Resumo do empacotamento");
        _console.Output.ShouldContain(CAMINHO_PACOTE);
    }

    [Fact]
    public async Task InvokeAsync_QuandoSilenciosoTrue_NaoDeveExibirMensagensDeInfoOuSucesso()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string SAIDA = @"C:\saida";
        const string CAMINHO_PACOTE = @"C:\saida\pacote.zip";

        _empacotadorService.Empacotar(PASTA, SAIDA, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(new EmpacotamentoResultado(CAMINHO_PACOTE, ["manifesto.dat", "arquivo1.txt"]));

        // Act
        var result = await _rootCommand.InvokeAsync
        ([
            "empacotar",
            "--pasta", PASTA,
            "--saida", SAIDA,
            "--silencioso", "true"
        ]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldNotContain("INFO");
        _console.Output.ShouldNotContain("SUCCESS");
    }

    [Fact]
    public async Task InvokeAsync_QuandoSemCorTrue_DeveDesabilitarAnsiConsole()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string SAIDA = @"C:\saida";
        const string CAMINHO_PACOTE = @"C:\saida\pacote.zip";

        _empacotadorService.Empacotar(PASTA, SAIDA, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(new EmpacotamentoResultado(CAMINHO_PACOTE, ["manifesto.dat", "arquivo1.txt"]));

        // Act
        var result = await _rootCommand.InvokeAsync
        ([
            "empacotar",
            "--pasta", PASTA,
            "--saida", SAIDA,
            "--sem-cor", "true"
        ]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("SUCCESS");
    }

    [Fact]
    public void ExibirResumoConsole_DeveExibirTabelaComPacoteENomesDosArquivos()
    {
        // Arrange
        var console = new TestConsole();
        var empacotadorService = Substitute.For<IEmpacotadorService>();
        var cmd = new EmpacotarCommand(_silenciosoOption, _semCorOption, _resumoOption, empacotadorService, console);
        var resultado = new EmpacotamentoResultado(@"C:\saida\pacote.cmpkg", ["manifesto.dat", "arquivo1.txt", "dados.csv"]);

        // Act
        var metodo = cmd.GetType().GetMethod("ExibirResumoConsole", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        metodo!.Invoke(cmd, [resultado]);

        // Assert
        var output = console.Output;
        output.ShouldContain("Resumo do Empacotamento");
        output.ShouldContain("Pacote Gerado");
        output.ShouldContain("Arquivos Incluídos");
        output.ShouldContain("pacote.cmpkg");
        output.ShouldContain("manifesto.dat");
        output.ShouldContain("arquivo1.txt");
        output.ShouldContain("dados.csv");
    }

    [Fact]
    public void ExibirResumoConsole_DeveExibirTabelaMesmoSemArquivosIncluidos()
    {
        // Arrange
        var console = new TestConsole();
        var empacotadorService = Substitute.For<IEmpacotadorService>();
        var cmd = new EmpacotarCommand(_silenciosoOption, _semCorOption, _resumoOption, empacotadorService, console);
        var resultado = new EmpacotamentoResultado(@"C:\saida\pacote.cmpkg", []);

        // Act
        var metodo = cmd.GetType().GetMethod("ExibirResumoConsole", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        metodo!.Invoke(cmd, [resultado]);

        // Assert
        var output = console.Output;
        output.ShouldContain("Resumo do Empacotamento");
        output.ShouldContain("Pacote Gerado");
        output.ShouldContain("Arquivos Incluídos");
        output.ShouldContain("pacote.cmpkg");
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoConsole_DeveExibirResumoConsole()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string SAIDA = @"C:\saida";
        const string CAMINHO_PACOTE = @"C:\saida\pacote.cmpkg";

        _empacotadorService.Empacotar(PASTA, SAIDA, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(new EmpacotamentoResultado(CAMINHO_PACOTE, ["manifesto.dat", "arquivo1.txt", "dados.csv"]));

        // Act
        var result = await _rootCommand.InvokeAsync
        ([
            "empacotar",
            "--pasta", PASTA,
            "--saida", SAIDA,
            "--resumo", "console"
        ]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("Resumo do Empacotamento");
        _console.Output.ShouldContain("Pacote Gerado");
        _console.Output.ShouldContain("Arquivos Incluídos");
        _console.Output.ShouldContain("pacote.cmpkg");
        _console.Output.ShouldContain("manifesto.dat");
        _console.Output.ShouldContain("arquivo1.txt");
        _console.Output.ShouldContain("dados.csv");
    }
}
