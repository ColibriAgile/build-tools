using BuildTools.Models;
using BuildTools.Services;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes.Services;

using System.IO.Abstractions.TestingHelpers;
using Spectre.Console.Testing;

/// <summary>
/// Testes unitários para EmpacotadorService.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EmpacotadorServiceTestes
{
    private readonly IZipService _zipService = Substitute.For<IZipService>();
    private readonly IManifestoService _manifestoService = Substitute.For<IManifestoService>();
    private readonly IManifestoGeradorService _manifestoGeradorService = Substitute.For<IManifestoGeradorService>();
    private readonly IArquivoService _arquivoService = Substitute.For<IArquivoService>();
    private readonly TestConsole _console = new();
    private readonly MockFileSystem _fileSystem = new();
    private readonly EmpacotadorService _service;

    public EmpacotadorServiceTestes()
        => _service = new EmpacotadorService(
            _zipService,
            _manifestoService,
            _manifestoGeradorService,
            _arquivoService,
            _console,
            _fileSystem
        );

    [Fact]
    public void Empacotar_DeveChamarTodosServicosENomearArquivoCorretamente()
    {
        // Arrange
        const string PASTA = @"C:\origem";
        const string PASTA_SAIDA = @"C:\saida";

        var manifestoOriginal = new Manifesto
        {
            Nome = "Meu Pacote",
            Versao = "1.2.3",
            Arquivos =
            []
        };

        var manifestoExpandido = new Manifesto
        {
            Nome = "Meu Pacote",
            Versao = "1.2.3",
            Arquivos =
            [
                new ManifestoArquivo
                {
                    Nome = "manifesto.dat"
                },
                new ManifestoArquivo
                {
                    Nome = "arquivo1.txt"
                }
            ]
        };

        _fileSystem.AddFile(@"C:\origem\manifesto.dat", new MockFileData("conteudo"));
        _fileSystem.AddFile(@"C:\origem\arquivo1.txt", new MockFileData("conteudo"));

        var arquivos = new List<string> { "manifesto.dat", "arquivo1.txt" };
        _manifestoService.LerManifesto(PASTA).Returns(manifestoOriginal);
        _manifestoGeradorService.GerarManifestoExpandido(PASTA, manifestoOriginal).Returns(manifestoExpandido);

        // Act
        var resultado = _service.Empacotar(PASTA, PASTA_SAIDA, senha: "senha123", versao: "1.2.3", develop: true);

        // Assert
        resultado.CaminhoPacote.ShouldBe(@"C:\saida\MeuPacote_1_2_3.cmpkg");
        resultado.ArquivosIncluidos.ShouldContain("manifesto.dat");
        resultado.ArquivosIncluidos.ShouldContain("arquivo1.txt");
        _manifestoService.Received(1).LerManifesto(PASTA);
        _manifestoGeradorService.Received(1).GerarManifestoExpandido(PASTA, manifestoOriginal);
        _manifestoService.Received(1).SalvarManifesto(PASTA, manifestoExpandido);

        _arquivoService.Received(1).ExcluirComPrefixo(PASTA_SAIDA, "MeuPacote_", ".cmpkg");

        _zipService.Received(1).CompactarZip
        (
            @"C:\origem",
            Arg.Is<List<string>>(l => l.SequenceEqual(arquivos)),
            @"C:\saida\MeuPacote_1_2_3.cmpkg",
            "senha123"
        );

        manifestoOriginal.Extras.ShouldNotBeNull();
        manifestoOriginal.Extras!["develop"].ShouldBe(true);
        manifestoOriginal.Versao.ShouldBe("1.2.3");
    }

    [Fact]
    public void Empacotar_QuandoVersaoNaoInformada_DeveManterVersaoDoManifesto()
    {
        // Arrange
        const string PASTA = @"C:\origem";
        const string PASTA_SAIDA = @"C:\saida";

        var manifestoOriginal = new Manifesto
        {
            Nome = "Pacote",
            Versao = "9.9.9",
            Arquivos =
            []
        };

        var manifestoExpandido = new Manifesto
        {
            Nome = "Pacote",
            Versao = "9.9.9",
            Arquivos =
            [
                new ManifestoArquivo
                {
                    Nome = "manifesto.dat"
                }
            ]
        };

        _fileSystem.AddFile(PASTA + @"\manifesto.dat", new MockFileData("conteudo"));
        _manifestoService.LerManifesto(PASTA).Returns(manifestoOriginal);
        _manifestoGeradorService.GerarManifestoExpandido(PASTA, manifestoOriginal).Returns(manifestoExpandido);

        // Act
        var resultado2 = _service.Empacotar(PASTA, PASTA_SAIDA);

        // Assert
        resultado2.CaminhoPacote.ShouldBe(@"C:\saida\Pacote_9_9_9.cmpkg");
        resultado2.ArquivosIncluidos.ShouldContain("manifesto.dat");
        manifestoOriginal.Versao.ShouldBe("9.9.9");
    }

    [Fact]
    public void Empacotar_SetaDevelopFalseQuandoNaoInformado()
    {
        // Arrange
        const string PASTA = @"C:\origem";
        const string PASTA_SAIDA = @"C:\saida";

        var manifestoOriginal = new Manifesto
        {
            Nome = "Pacote",
            Versao = "9.9.0",
            Arquivos =
            []
        };

        var manifestoExpandido = new Manifesto
        {
            Nome = "Pacote",
            Versao = "9.9.0",
            Arquivos =
            [
                new ManifestoArquivo
                {
                    Nome = "manifesto.dat"
                }
            ]
        };

        _fileSystem.AddFile(PASTA + @"\manifesto.dat", new MockFileData("conteudo"));
        _manifestoService.LerManifesto(PASTA).Returns(manifestoOriginal);
        _manifestoGeradorService.GerarManifestoExpandido(PASTA, manifestoOriginal).Returns(manifestoExpandido);

        // Act
        var resultado3 = _service.Empacotar(PASTA, PASTA_SAIDA);

        // Assert
        resultado3.CaminhoPacote.ShouldBe(@"C:\saida\Pacote_9_9_0.cmpkg");
        manifestoOriginal.Extras.ShouldNotBeNull();
        manifestoOriginal.Extras!["develop"].ShouldBe(false);
    }

    [Fact]
    public void Empacotar_DeveEmitirWarnQuandoArquivosNaoIncluidos()
    {
        // Arrange
        const string PASTA = @"C:\origem";
        const string PASTA_SAIDA = @"C:\saida";

        // Cria arquivos na pasta de origem
        _fileSystem.AddFile(@"C:\origem\manifesto.server", new MockFileData("conteudo"));
        _fileSystem.AddFile(@"C:\origem\nao_incluido.txt", new MockFileData("extra"));

        var manifestoOriginal = new Manifesto
        {
            Nome = "Pacote",
            Versao = "1.0.0",
            Arquivos = []
        };

        var manifestoExpandido = new Manifesto
        {
            Nome = "Pacote",
            Versao = "1.0.0",
            Arquivos = [new ManifestoArquivo { Nome = "manifesto.dat" }]
        };

        _manifestoService.LerManifesto(PASTA).Returns(manifestoOriginal);
        _manifestoGeradorService.GerarManifestoExpandido(PASTA, manifestoOriginal).Returns(manifestoExpandido);

        // Act
        _service.Empacotar(PASTA, PASTA_SAIDA);

        // Assert
        _console.Output.ShouldContain("nao_incluido.txt");
        _console.Output.ShouldNotContain("manifesto.server");
        _console.Output.ShouldContain("WARN");
    }
}
