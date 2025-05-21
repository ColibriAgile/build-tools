using BuildTools.Commands;
using BuildTools.Services;
using Spectre.Console;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes.Commands;

[ExcludeFromCodeCoverage]
public sealed class EmpacotarScriptsCommandTestes
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IZipService _zipService = Substitute.For<IZipService>();
    private readonly IAnsiConsole _console = Substitute.For<IAnsiConsole>();
    private readonly IEmpacotadorScriptsService _service;
    private readonly Option<bool> _silenciosoOption = new("--silencioso");
    private readonly Option<bool> _semCorOption = new("--sem-cor");
    private readonly Option<bool> _resumoOption = new("--resumo");
    private readonly EmpacotarScriptsCommand _cmd;

    public EmpacotarScriptsCommandTestes()
    {
        _service = Substitute.For<IEmpacotadorScriptsService>();
        _cmd = new EmpacotarScriptsCommand(_silenciosoOption, _semCorOption, _resumoOption, _fileSystem, _console, _zipService, _service);
    }

    [Fact]
    public async Task InvokeAsync_QuandoPastaNaoExiste_DeveExibirErro()
    {
        // Arrange
        const string PASTA = "C:/naoexiste";
        _fileSystem.Directory.Exists(Arg.Any<string>()).Returns(false);
        var root = new RootCommand();
        root.AddCommand(_cmd);

        // Act
        var result = await root.InvokeAsync(["empacotar_scripts", "--pasta", PASTA, "--saida", "C:/saida"]);

        // Assert
        result.ShouldNotBe(0); // Deve retornar erro
        _console.ReceivedWithAnyArgs().MarkupLineInterpolated($"[red][[ERROR]] A pasta de origem não existe: {PASTA}[/]");
    }

    [Fact]
    public async Task InvokeAsync_QuandoPastaExiste_DeveExecutarComSucesso()
    {
        // Arrange
        const string PASTA_ORIGEM = "C:/pasta";
        const string PASTA_SAIDA = "C:/saida";
        const string ZIP = "C:/pasta/_scripts.zip";
        _fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
        _fileSystem.Directory.CreateDirectory(Arg.Any<string>()).Returns(Substitute.For<IDirectoryInfo>());
        _fileSystem.Path.Combine(Arg.Any<string>(), "_scripts.zip").Returns(ZIP);
        _service.TemConfigJson(Arg.Any<string>()).Returns(true);
        var arquivos = new List<(string caminhoCompleto, string caminhoNoZip)> { ("arq.sql", "arq.sql") };
        _service.ListarArquivosComRelativo(Arg.Any<string>()).Returns(arquivos);
        var root = new RootCommand();
        root.AddCommand(_cmd);

        // Act
        var result = await root.InvokeAsync(["empacotar_scripts", "--pasta", PASTA_ORIGEM, "--saida", PASTA_SAIDA]);

        // Assert
        result.ShouldBe(0); // Sucesso
        _fileSystem.Directory.Exists("C:/pasta").ShouldBeTrue();

        _zipService
            .Received()
            .CompactarZip
            (
                PASTA_ORIGEM,
                Arg.Is<List<(string caminhoCompleto, string caminhoNoZip)>>
                (
                    arqs => arqs.Count == 1
                        && arqs[0].caminhoCompleto == arquivos[0].caminhoCompleto
                        && arqs[0].caminhoNoZip == arquivos[0].caminhoNoZip
                ),
                ZIP
            );
    }
}
