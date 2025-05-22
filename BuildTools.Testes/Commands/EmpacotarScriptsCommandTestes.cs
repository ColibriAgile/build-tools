using BuildTools.Commands;
using BuildTools.Services;
using Spectre.Console.Testing;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes.Commands;

[ExcludeFromCodeCoverage]
public sealed class EmpacotarScriptsCommandTestes
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IZipService _zipService = Substitute.For<IZipService>();
    private readonly TestConsole _console = new();
    private readonly IEmpacotadorScriptsService _service;
    private readonly Option<bool> _silenciosoOption = new(aliases: ["--silencioso", "-s"]);
    private readonly Option<bool> _semCorOption = new(aliases: ["--sem-cor", "-sc"]);
    private readonly Option<string> _resumoOption = new(aliases: ["--resumo", "-r"]);
    private readonly RootCommand _rootCommand = new("Colibri BuildTools - Empacotador de soluções");

    public EmpacotarScriptsCommandTestes()
    {
        _service = Substitute.For<IEmpacotadorScriptsService>();
        var cmd = new EmpacotarScriptsCommand(_silenciosoOption, _semCorOption, _resumoOption, _fileSystem, _console, _zipService, _service);
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
    public async Task InvokeAsync_QuandoPastaExiste_DeveExecutarComSucesso()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        const string ZIP = @"C:\pasta\_scripts.zip";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _fileSystem.File.Exists(ZIP).Returns(false);
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(arquivos);

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldBe(0); // Sucesso
        _console.Output.ShouldContain("SUCCESS");
    }

    [Fact]
    public async Task InvokeAsync_QuandoZipDestinoExiste_DeveApagarZip()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        const string ZIP = @"C:\pasta\_scripts.zip";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(true);
        _fileSystem.File.Exists(Path.Combine(PASTA_SAIDA, "_scripts.zip")).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _fileSystem.File.Exists(ZIP).Returns(false);
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(arquivos);

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldBe(0); // Sucesso
        _fileSystem.File.Received().Delete(Path.Combine(PASTA_SAIDA, "_scripts.zip"));
    }

    [Fact]
    public async Task InvokeAsync_QuandoNaoHaArquivosNaPasta_DeveExibirAviso()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        const string ZIP = @"C:\pasta\_scripts.zip";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _fileSystem.File.Exists(ZIP).Returns(false);
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(new List<(string, string)>());

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("Nenhum arquivo de script encontrado");
    }

    [Fact]
    public async Task InvokeAsync_QuandoPadronizarNomesFalse_NaoDeveChamarPadronizarNomes()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        const string ZIP = @"C:\pasta\_scripts.zip";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _fileSystem.File.Exists(ZIP).Returns(false);
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(arquivos);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "empacotar_scripts",
                "--pasta",
                PASTA_ORIGEM,
                "--saida",
                PASTA_SAIDA,
                "--padronizar_nomes",
                "false"
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldNotContain("renomeado");
    }

    [Fact]
    public async Task InvokeAsync_QuandoSubpastasValidas_DeveEmpacotarCadaSubpasta()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        var subpastas = new[] { @"C:\pasta\01", @"C:\pasta\02" };
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.TemConfigJson(PASTA_ORIGEM).Returns(false);
        _service.ListarSubpastasValidas(PASTA_ORIGEM).Returns(subpastas);
        _service.ListarArquivosComRelativo(@"C:\pasta/01").Returns(new List<(string, string)> { ("arq.sql", "arq.sql") });
        _service.ListarArquivosComRelativo(@"C:\pasta/02").Returns(new List<(string, string)> { ("arq.sql", "arq.sql") });

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("SUCCESS");
    }

    [Fact]
    public async Task InvokeAsync_QuandoCriarPastaSaidaFalha_DeveExibirErro()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(false);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(static _ => throw new UnauthorizedAccessException("Sem permissão"));
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldBe(1);
        _console.Output.ShouldContain("Sem permissão");
    }

    [Fact]
    public async Task InvokeAsync_QuandoProcessarEmpacotamentoLancaExcecao_DeveExibirErro()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(static _ => throw new Exception("Falha interna"));

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldNotBe(0);
        _console.Output.ShouldContain("ERROR");
    }

    [Fact]
    public async Task InvokeAsync_QuandoPadronizarNomesException_ImprimeErro()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(arquivos);
        _fileSystem.File.WhenForAnyArgs(static f => f.Move("", "", overwrite: true)).Throws(new IOException("Erro ao mover arquivo"));

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "empacotar_scripts",
                "--pasta",
                PASTA_ORIGEM,
                "--saida",
                PASTA_SAIDA,
                "--padronizar_nomes",
                "true"
            ]
        );

        // Assert
        result.ShouldBe(1);
        _console.Output.ShouldContain("Erro ao renomear arquivo");
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoTrue_DeveExibirResumoMarkdown()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(arquivos);

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA, "--resumo", "markdown"]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("Resumo dos pacotes gerados");
    }

    [Fact]
    public async Task InvokeAsync_QuandoPastaSaidaJaExiste_NaoDeveCriarNovamente()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(true);
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(new List<(string, string)> { ("arq.sql", "arq.sql") });

        // Act
        var result = await _rootCommand.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("SUCCESS");
    }

    [Fact]
    public async Task InvokeAsync_QuandoSilenciosoTrue_NaoDeveExibirMensagens()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(arquivos);

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
        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.TemConfigJson(PASTA_ORIGEM).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA_ORIGEM).Returns(arquivos);

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
        _console.Output.ShouldContain("SUCCESS");
    }

    [Fact]
    public async Task InvokeAsync_QuandoPadronizarNomesComSubpastasValidas_DevePadronizarNomesArquivos()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        const string PASTA_1 = "01_dconnect_pgsql";
        const string PASTA_2 = "02_dconnect_mssql";
        const string PASTA_3 = "05_master";
        const string PASTA_4 = "uteis";

        string[] subpastas =
        [
            $"{PASTA_ORIGEM}\\{PASTA_1}",
            $"{PASTA_ORIGEM}\\{PASTA_2}",
            $"{PASTA_ORIGEM}\\{PASTA_3}",
            $"{PASTA_ORIGEM}\\{PASTA_4}"
        ];

        _fileSystem.Directory.Exists(PASTA_ORIGEM).Returns(true);
        _fileSystem.Directory.Exists(PASTA_SAIDA).Returns(true);
        _fileSystem.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        _service.TemConfigJson(PASTA_ORIGEM).Returns(false);
        _service.ListarSubpastasValidas(PASTA_ORIGEM).Returns(subpastas.SkipLast(1));

        List<(string, string)> arquivos01 = [($"{subpastas[0]}\\001.sql", "001.sql"), ($"{subpastas[0]}\\002.sql", "002.sql")];
        List<(string, string)> arquivos02 = [($"{subpastas[1]}\\001.sql", "001.sql"), ($"{subpastas[1]}\\002.sql", "002.sql")];
        List<(string, string)> arquivos03 = [($"{subpastas[2]}\\001.sql", "001.sql"), ($"{subpastas[2]}\\002.sql", "002.sql")];
        List<(string, string)> arquivos04 = [($"{subpastas[3]}\\001.sql", "001.sql"), ($"{subpastas[3]}\\002.sql", "002.sql")];

        _service.ListarArquivosComRelativo(subpastas[0]).Returns(arquivos01);
        _service.ListarArquivosComRelativo(subpastas[1]).Returns(arquivos02);
        _service.ListarArquivosComRelativo(subpastas[2]).Returns(arquivos03);
        _service.ListarArquivosComRelativo(subpastas[3]).Returns(arquivos04);

        // Act
        var result = await _rootCommand.InvokeAsync
        ([
            "empacotar_scripts",
            "--pasta", PASTA_ORIGEM,
            "--saida", PASTA_SAIDA,
            "--padronizar_nomes", "true"
        ]);

        // Assert
        result.ShouldBe(0);
        _fileSystem.File.Received().Move(@"C:\saida\_scripts01_dconnect_pgsql.zip", @"C:\saida\scripts_dconnect_pgsql.zip", overwrite: true);
        _fileSystem.File.Received().Move(@"C:\saida\_scripts02_dconnect_mssql.zip", @"C:\saida\scripts_dconnect_mssql.zip", overwrite: true);
        _fileSystem.File.Received().Move(@"C:\saida\_scripts05_master.zip", @"C:\saida\scripts_master.zip", overwrite: true);
        _console.Output.ShouldContain("SUCCESS");
        _console.Output.ShouldNotContain("uteis");
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoConsole_DeveExibirResumoConsole()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string SAIDA = @"C:\saida";
        const string ZIP = @"C:\saida\_scripts.zip";
        _fileSystem.Directory.Exists(PASTA).Returns(true);
        _fileSystem.Directory.Exists(SAIDA).Returns(true);
        _fileSystem.File.Exists(ZIP).Returns(false);
        _fileSystem.Directory.GetCurrentDirectory().Returns(PASTA);
        _service.TemConfigJson(PASTA).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(PASTA).Returns(arquivos);

        // Act
        var result = await _rootCommand.InvokeAsync
        ( [
            "empacotar_scripts",
            "--pasta", PASTA,
            "--saida", SAIDA,
            "--resumo", "console"
        ] );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("Resumo dos pacotes gerados");
        _console.Output.ShouldContain("_scripts.zip");
    }
}
