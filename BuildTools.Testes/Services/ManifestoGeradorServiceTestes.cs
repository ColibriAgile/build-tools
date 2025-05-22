using System.Diagnostics.CodeAnalysis;
using BuildTools.Models;
using BuildTools.Services;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para o serviço de geração de manifesto expandido (ManifestoGeradorService).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ManifestoGeradorServiceTestes
{
    [Fact]
    public void GerarManifestoExpandido_ComNomesEPatterns_DeveExpandirCorretamente()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var service = new ManifestoGeradorService(fileSystem);
        const string PASTA = @"C:\pasta";
        string[] arquivosDiretorio = ["manifesto.dat", "dados_01.csv", "dados_02.csv", "outro.txt", "abobrinha.zip"];
        fileSystem.Directory.GetFiles(PASTA).Returns(arquivosDiretorio);

        var manifesto = new Manifesto
        {
            Nome = "PacoteTeste",
            Versao = "1.0.0",
            Arquivos =
            [
                new ManifestoArquivo { Nome = "outro.txt" },
                new ManifestoArquivo { PatternNome = @"^dados_\d+\.csv$" }
            ]
        };

        // Act
        var manifestoExpandido = service.GerarManifestoExpandido(PASTA, manifesto);

        // Assert
        manifestoExpandido.Arquivos.ShouldContain(static a => a.Nome == "manifesto.dat");
        manifestoExpandido.Arquivos.ShouldContain(static a => a.Nome == "outro.txt");
        manifestoExpandido.Arquivos.ShouldContain(static a => a.Nome == "dados_01.csv");
        manifestoExpandido.Arquivos.ShouldContain(static a => a.Nome == "dados_02.csv");
        manifestoExpandido.Arquivos.Count.ShouldBe(4);
        manifestoExpandido.Arquivos[0].Nome.ShouldBe("manifesto.dat");
    }

    [Fact]
    public void GerarManifestoExpandido_SemMatchPattern_DeveLancarExcecao()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var service = new ManifestoGeradorService(fileSystem);
        const string PASTA = @"C:\pasta";
        string[] arquivosDiretorio = ["manifesto.dat", "outro.txt"];
        fileSystem.Directory.GetFiles(PASTA).Returns(arquivosDiretorio);

        var manifesto = new Manifesto
        {
            Nome = "PacoteTeste",
            Versao = "1.0.0",
            Arquivos = [
                new ManifestoArquivo { PatternNome = @"^dados_\d+\.csv$" },
                new ManifestoArquivo { Nome = "outro.txt" }
            ]
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.GerarManifestoExpandido(PASTA, manifesto))
            .Message.ShouldBe("Nenhum arquivo encontrado para o pattern previsto no manifesto: ^dados_\\d+\\.csv$");
    }

    [Fact]
    public void GerarManifestoExpandido_ComDuplicidade_DeveLancarExcecaoSePatternNaoEncontra()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var service = new ManifestoGeradorService(fileSystem);
        const string PASTA = @"C:\pasta";
        string[] arquivosDiretorio = ["manifesto.dat", "arq.txt"];
        fileSystem.Directory.GetFiles(PASTA).Returns(arquivosDiretorio);

        var manifesto = new Manifesto
        {
            Nome = "PacoteTeste",
            Versao = "1.0.0",
            Arquivos = [
                new ManifestoArquivo { Nome = "arq.txt" },
                new ManifestoArquivo { PatternNome = @"^arq\.txt$" }
            ]
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.GerarManifestoExpandido(PASTA, manifesto))
            .Message.ShouldBe("Nenhum arquivo encontrado para o pattern previsto no manifesto: ^arq\\.txt$");
    }

    [Fact]
    public void GerarManifestoExpandido_SempreIncluiManifestoDatComoPrimeiro()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var service = new ManifestoGeradorService(fileSystem);
        const string PASTA = @"C:\pasta";
        string[] arquivosDiretorio = ["manifesto.dat", "a.txt"];
        fileSystem.Directory.GetFiles(PASTA).Returns(arquivosDiretorio);

        var manifesto = new Manifesto
        {
            Nome = "PacoteTeste",
            Versao = "1.0.0",
            Arquivos = [new ManifestoArquivo { Nome = "a.txt" }]
        };

        // Act
        var manifestoExpandido = service.GerarManifestoExpandido(PASTA, manifesto);

        // Assert
        manifestoExpandido.Arquivos[0].Nome.ShouldBe("manifesto.dat");
    }

    [Fact]
    public void GerarManifestoExpandido_ArquivoPorNomeNaoExiste_DeveLancarExcecao()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var service = new ManifestoGeradorService(fileSystem);
        const string PASTA = @"C:\pasta";
        string[] arquivosDiretorio = ["manifesto.dat", "outro.txt"];
        fileSystem.Directory.GetFiles(PASTA).Returns(arquivosDiretorio);

        var manifesto = new Manifesto
        {
            Nome = "PacoteTeste",
            Versao = "1.0.0",
            Arquivos = [new ManifestoArquivo { Nome = "nao_existe.txt" }]
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.GerarManifestoExpandido(PASTA, manifesto))
            .Message.ShouldBe("Arquivo previsto no manifesto não encontrado: nao_existe.txt");
    }

    [Fact]
    public void GerarManifestoExpandido_PatternNaoEncontraNenhumArquivo_DeveLancarExcecao()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var service = new ManifestoGeradorService(fileSystem);
        const string PASTA = @"C:\pasta";
        string[] arquivosDiretorio = ["manifesto.dat", "outro.txt"];
        fileSystem.Directory.GetFiles(PASTA).Returns(arquivosDiretorio);

        var manifesto = new Manifesto
        {
            Nome = "PacoteTeste",
            Versao = "1.0.0",
            Arquivos = [new ManifestoArquivo { PatternNome = @"^dados_\d+\.csv$" }]
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.GerarManifestoExpandido(PASTA, manifesto))
            .Message.ShouldBe("Nenhum arquivo encontrado para o pattern previsto no manifesto: ^dados_\\d+\\.csv$");
    }

    [Fact]
    public void GerarManifestoExpandido_ComDuplicidadePatternSemMatch_DeveLancarExcecao()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var service = new ManifestoGeradorService(fileSystem);
        const string PASTA = @"C:\pasta";
        string[] arquivosDiretorio = ["manifesto.dat", "arq.txt"];
        fileSystem.Directory.GetFiles(PASTA).Returns(arquivosDiretorio);

        var manifesto = new Manifesto
        {
            Nome = "PacoteTeste",
            Versao = "1.0.0",
            Arquivos = [
                new ManifestoArquivo { Nome = "arq.txt" },
                new ManifestoArquivo { PatternNome = @"^nao_existe\.txt$" }
            ]
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.GerarManifestoExpandido(PASTA, manifesto))
            .Message.ShouldBe("Nenhum arquivo encontrado para o pattern previsto no manifesto: ^nao_existe\\.txt$");
    }
}
