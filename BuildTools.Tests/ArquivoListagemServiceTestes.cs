using System.Diagnostics.CodeAnalysis;
using BuildTools.Models;
using BuildTools.Services;

namespace BuildTools.Testes;

/// <summary>
/// Testes unitários para o serviço de listagem de arquivos (ArquivoListagemService).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ArquivoListagemServiceTestes
{
    private const string NOME_PADRAO = "PacoteTeste";
    private const string VERSAO_PADRAO = "1.0.0";
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ArquivoListagemService _service;

    public ArquivoListagemServiceTestes()
    {
        _fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(static x => x.Arg<string>());
        _service = new ArquivoListagemService(_fileSystem);
    }

    [Fact]
    public void ObterArquivos_ComManifestoVazio_DeveRetornarSomenteManifestoDat()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos = []
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns([]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldBe(["manifesto.dat"]);
    }

    [Fact]
    public void ObterArquivos_ComArquivosNoDiretorio_DeveRetornarArquivosMaisManifestoDat()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos = []
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns(["arquivo1.txt", "manifesto.dat", "arquivo2.dll"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldBe(["manifesto.dat", "arquivo1.txt", "arquivo2.dll"]);
    }

    [Fact]
    public void ObterArquivos_ArquivosComManifestoServer_DeveIgnorarManifestoServer()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos = []
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns(["manifesto.server", "manifesto.dat", "arquivo.dll"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldBe(["manifesto.dat", "arquivo.dll"]);
    }

    [Fact]
    public void ObterArquivos_DeveRetornarManifestoDatComoPrimeiro()
    {
        // Arrange
        const string PASTA = "C:/fake";
        var arquivos = new[] { "manifesto.dat", "arquivo1.txt", "arquivo2.exe" };
        _fileSystem.Directory.GetFiles(PASTA).Returns(arquivos);

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos = []
        };

        // Act
        var resultado = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        resultado.ShouldNotBeEmpty();
        resultado[0].ShouldBe("manifesto.dat");
        resultado.ShouldContain("arquivo1.txt");
        resultado.ShouldContain("arquivo2.exe");
    }

    [Fact]
    public void ObterArquivos_DeveAtribuirDestinoClientParaExe()
    {
        // Arrange
        const string PASTA = "C:/fake";
        var arquivos = new[] { "manifesto.dat", "app.exe" };
        _fileSystem.Directory.GetFiles(PASTA).Returns(arquivos);

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos = []
        };

        // Act
        _service.ObterArquivos(PASTA, manifesto);

        // Assert
        manifesto.Arquivos.ShouldContain(static a => a.Nome == "app.exe" && a.Destino == "client");
    }

    [Fact]
    public void ObterArquivos_ArquivoComPatternNomeNoManifesto_DeveAssociarArquivoCorretamente()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos =
            [
                new ManifestoArquivo { PatternNome = @"^dados_\d+\.csv$" }
            ]
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns(["dados_123.csv", "manifesto.dat"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldBe(["manifesto.dat", "dados_123.csv"]);
        manifesto.Arquivos.ShouldContain(static a => a.Nome == "dados_123.csv");
    }

    [Fact]
    public void ObterArquivos_ArquivosDuplicados_DeveRetornarApenasUmPorNome()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos = []
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns(["manifesto.dat", "arquivo.txt", "arquivo.txt"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.Count(static a => a == "arquivo.txt").ShouldBe(1);
    }

    [Fact]
    public void ObterArquivos_ArquivoExeSemDestino_DeveAtribuirDestinoClient()
    {
        // Arrange
        const string PASTA = "C:/fake";
        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos = []
        };
        _fileSystem.Directory.GetFiles(PASTA).Returns(["manifesto.dat", "app.exe"]);

        // Act
        _service.ObterArquivos(PASTA, manifesto);

        // Assert
        manifesto.Arquivos.ShouldContain(static a => a.Nome == "app.exe" && a.Destino == "client");
    }

    [Fact]
    public void ObterArquivos_ArquivosComOrdernacao_DeveRetornarNaOrdemCorreta()
    {
        // Arrange
        const string PASTA = "C:/fake";
        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos =
            [
                new ManifestoArquivo { Nome = "pacote.zip", Destino = "pacote" },
                new ManifestoArquivo { Nome = "scripts1.zip", Destino = "scripts" },
                new ManifestoArquivo { Nome = "shared.dll", Destino = "shared" },
                new ManifestoArquivo { Nome = "server.dll", Destino = "server" },
                new ManifestoArquivo { Nome = "client.dll", Destino = "client" }
            ]
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns(["pacote.zip", "scripts1.zip", "shared.dll", "server.dll", "client.dll", "manifesto.dat"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldBe(["manifesto.dat", "pacote.zip", "scripts1.zip", "shared.dll", "server.dll", "client.dll"]);
    }

    [Fact]
    public void ObterArquivos_ArquivoPatternSemNomeNoManifesto_DeveAdicionarArquivoAoManifesto()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos =
            [
                new ManifestoArquivo { PatternNome = @"^log_\d+\.txt$" } // pattern sem nome
            ]
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns(["log_20250101.txt", "manifesto.dat"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldBe(["manifesto.dat", "log_20250101.txt"]);
        manifesto.Arquivos.ShouldContain(static a => a.Nome == "log_20250101.txt");
    }

    [Fact]
    public void ObterArquivos_ArquivoPatternSemMatchNosDiretorio_NaoDeveAdicionarAoManifesto()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifestoArquivoOriginal = new ManifestoArquivo
        {
            PatternNome = @"^semCorrespondencia_\d+\.xlsx$",
            Destino = "client"
        };

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos =
            [
                manifestoArquivoOriginal // Padrão que não corresponde a nenhum arquivo
            ]
        };

        _fileSystem.Directory.GetFiles(PASTA).Returns(["outroArquivo.txt", "manifesto.dat"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldBe(["manifesto.dat", "outroArquivo.txt"]); // Os arquivos na pasta + manifesto

        // Verifica que apenas os arquivos reais estão no manifesto (e o pattern sem match foi descartado)
        manifesto.Arquivos.Count.ShouldBe(1); // Apenas o arquivo outroArquivo.txt
        manifesto.Arquivos.ShouldContain(static a => a.Nome == "outroArquivo.txt");
        manifesto.Arquivos.ShouldNotContain(static a => a.PatternNome == @"^semCorrespondencia_\d+\.xlsx$");
    }

    [Fact]
    public void ObterArquivos_ArquivoPatternCorrespondeMultiplosArquivos_DeveAssociarPeloMenosUm()
    {
        // Arrange
        const string PASTA = "C:/fake";

        var manifesto = new Manifesto
        {
            Nome = NOME_PADRAO,
            Versao = VERSAO_PADRAO,
            Arquivos =
            [
                new ManifestoArquivo
                {
                    PatternNome = @"^relatorio\d+\.csv$",
                    Destino = "reports"
                }
            ]
        };

        // Vários arquivos que correspondem ao padrão
        _fileSystem.Directory.GetFiles(PASTA).Returns(["manifesto.dat", "relatorio1.csv", "relatorio2.csv", "outros.txt"]);

        // Act
        var arquivos = _service.ObterArquivos(PASTA, manifesto);

        // Assert
        arquivos.ShouldContain("relatorio1.csv");
        arquivos.ShouldContain("outros.txt");
        arquivos.ShouldContain("manifesto.dat");

        // Verifica que pelo menos um arquivo correspondente foi adicionado com o destino correto
        manifesto.Arquivos.ShouldContain(static a => a.Nome!.StartsWith("relatorio") && a.Destino == "reports");
    }
}
