using System.Diagnostics.CodeAnalysis;
using BuildTools.Models;
using BuildTools.Resumos;
using Spectre.Console.Testing;

namespace BuildTools.Testes.Resumos;

/// <summary>
/// Testes para ResumoDeployMarkdown.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumoDeployMarkdownTestes
{
    [Fact]
    public void ExibirRelatorio_QuandoTodosArquivosEnviados_DeveExibirResumoCorreto()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoSucesso();
        var resumo = new ResumoDeployMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("# Relatório de Deploy");
        output.ShouldContain("## Resumo");
        output.ShouldContain("- **Ambiente**: desenvolvimento");
        output.ShouldContain("- **URL Marketplace**: https://marketplace.exemplo.com");
        output.ShouldContain("- **Simulado**: Não");
        output.ShouldContain("- **Tempo de Execução**: 5,2s");
        output.ShouldContain("- **Arquivos Enviados**: 2");
        output.ShouldContain("- **Arquivos Ignorados**: 0");
        output.ShouldContain("- **Arquivos com Falha**: 0");
        output.ShouldContain("## Arquivos Enviados com Sucesso");
        output.ShouldContain("### pacote1.cmpkg");
        output.ShouldContain("- **Pacote**: MeuPacote");
        output.ShouldContain("- **Versão**: 1.0.0");
        output.ShouldContain("- **Desenvolvimento**: Não");
        output.ShouldContain("- **URL S3**: [pacote1.cmpkg](https://s3.amazonaws.com/bucket/pacote1.cmpkg)");
    }

    [Fact]
    public void ExibirRelatorio_QuandoComFalhas_DeveExibirSecaoFalhas()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoComFalhas();
        var resumo = new ResumoDeployMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("# Relatório de Deploy");
        output.ShouldContain("- **Arquivos com Falha**: 1");
        output.ShouldContain("## Arquivos com Falha");
        output.ShouldContain("- **pacote2.cmpkg**: Falha no upload para S3");
    }

    [Fact]
    public void ExibirRelatorio_QuandoComIgnorados_DeveExibirSecaoIgnorados()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoComIgnorados();
        var resumo = new ResumoDeployMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("# Relatório de Deploy");
        output.ShouldContain("- **Arquivos Ignorados**: 1");
        output.ShouldContain("## Arquivos Ignorados");
        output.ShouldContain("- **pacote3.cmpkg**: Arquivo já existe no S3");
    }

    [Fact]
    public void ExibirRelatorio_QuandoSimulado_DeveExibirSimuladoSim()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoSimulado();
        var resumo = new ResumoDeployMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("- **Simulado**: Sim");
    }

    [Fact]
    public void ExibirRelatorio_QuandoManifestoComEmpresa_DeveExibirEmpresa()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoComEmpresa();
        var resumo = new ResumoDeployMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("- **Empresa**: ACME");
    }

    [Fact]
    public void ExibirRelatorio_QuandoSemArquivosEnviados_NaoDeveExibirSecaoEnviados()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoVazio();
        var resumo = new ResumoDeployMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("# Relatório de Deploy");
        output.ShouldNotContain("## Arquivos Enviados com Sucesso");
        output.ShouldNotContain("## Arquivos com Falha");
        output.ShouldNotContain("## Arquivos Ignorados");
    }

    private static DeployResultado CriarDeployResultadoSucesso()
        => new()
        {
            ArquivosEnviados =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote1.cmpkg",
                    NomeArquivoS3 = "pacote1.cmpkg",
                    UrlS3 = "https://s3.amazonaws.com/bucket/pacote1.cmpkg",
                    Manifesto = new ManifestoDeploy
                    {
                        Nome = "MeuPacote",
                        Versao = "1.0.0",
                        Develop = false
                    }
                },
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote2.cmpkg",
                    NomeArquivoS3 = "pacote2.cmpkg",
                    UrlS3 = "https://s3.amazonaws.com/bucket/pacote2.cmpkg",
                    Manifesto = new ManifestoDeploy
                    {
                        Nome = "OutroPacote",
                        Versao = "2.1.0",
                        Develop = true
                    }
                }
            ],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(5.2)
        };

    private static DeployResultado CriarDeployResultadoComFalhas()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados = [],
            ArquivosFalharam =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote2.cmpkg",
                    NomeArquivoS3 = "pacote2.cmpkg",
                    MensagemErro = "Falha no upload para S3"
                }
            ],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(3.1)
        };

    private static DeployResultado CriarDeployResultadoComIgnorados()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote3.cmpkg",
                    NomeArquivoS3 = "pacote3.cmpkg",
                    MensagemErro = "Arquivo já existe no S3"
                }
            ],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(1.5)
        };

    private static DeployResultado CriarDeployResultadoSimulado()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = true,
            TempoExecucao = TimeSpan.FromSeconds(0.1)
        };

    private static DeployResultado CriarDeployResultadoComEmpresa()
        => new()
        {
            ArquivosEnviados =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote1.cmpkg",
                    NomeArquivoS3 = "pacote1.cmpkg",
                    UrlS3 = "https://s3.amazonaws.com/bucket/pacote1.cmpkg",
                    Manifesto = new ManifestoDeploy
                    {
                        Nome = "PacoteEmpresa",
                        Versao = "1.0.0",
                        Develop = false,
                        SiglaEmpresa = "ACME"
                    }
                }
            ],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "producao",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(2.3)
        };

    private static DeployResultado CriarDeployResultadoVazio()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(0.1)
        };
}

/// <summary>
/// Testes para ResumoDeployConsole.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumoDeployConsoleTestes
{
    [Fact]
    public void ExibirRelatorio_QuandoTodosArquivosEnviados_DeveExibirResumoCorreto()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoSucesso();
        var resumo = new ResumoDeployConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("Propriedade");
        output.ShouldContain("Valor");
        output.ShouldContain("Ambiente");
        output.ShouldContain("desenvolvimento");
        output.ShouldContain("URL Marketplace");
        output.ShouldContain("https://marketplace.exemplo.com");
        output.ShouldContain("Simulado");
        output.ShouldContain("Não");
        output.ShouldContain("Tempo de Execução");
        output.ShouldContain("5,2s");
        output.ShouldContain("Arquivos Enviados");
        output.ShouldContain("2");
        output.ShouldContain("Arquivos Ignorados");
        output.ShouldContain("0");
        output.ShouldContain("Arquivos com Falha");
        output.ShouldContain("0");
    }

    [Fact]
    public void ExibirRelatorio_QuandoComArquivosEnviados_DeveExibirListaArquivos()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoSucesso();
        var resumo = new ResumoDeployConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("Arquivos enviados com sucesso:");
        output.ShouldContain("✓");
        output.ShouldContain("pacote1.cmpkg");
        output.ShouldContain("(MeuPacote v1.0.0)");
        output.ShouldContain("URL: https://s3.amazonaws.com/bucket/pacote1.cmpkg");
        output.ShouldContain("pacote2.cmpkg");
        output.ShouldContain("(OutroPacote v2.1.0)");
    }

    [Fact]
    public void ExibirRelatorio_QuandoComFalhas_DeveExibirListaFalhas()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoComFalhas();
        var resumo = new ResumoDeployConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("Arquivos com falha:");
        output.ShouldContain("✗");
        output.ShouldContain("pacote2.cmpkg");
        output.ShouldContain("Falha no upload para S3");
    }

    [Fact]
    public void ExibirRelatorio_QuandoComIgnorados_DeveExibirListaIgnorados()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoComIgnorados();
        var resumo = new ResumoDeployConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("Arquivos ignorados:");
        output.ShouldContain("⚠");
        output.ShouldContain("pacote3.cmpkg");
        output.ShouldContain("Arquivo já existe no S3");
    }

    [Fact]
    public void ExibirRelatorio_QuandoSimulado_DeveExibirSimuladoSim()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoSimulado();
        var resumo = new ResumoDeployConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("Simulado");
        output.ShouldContain("Sim");
    }

    [Fact]
    public void ExibirRelatorio_QuandoSemArquivosEnviados_NaoDeveExibirSecaoEnviados()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = CriarDeployResultadoVazio();
        var resumo = new ResumoDeployConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldNotContain("Arquivos enviados com sucesso:");
        output.ShouldNotContain("Arquivos com falha:");
        output.ShouldNotContain("Arquivos ignorados:");
    }

    [Fact]
    public void ExibirRelatorio_QuandoArquivoSemUrl_NaoDeveExibirUrl()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = new DeployResultado
        {
            ArquivosEnviados =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote1.cmpkg",
                    NomeArquivoS3 = "pacote1.cmpkg",
                    UrlS3 = null, // Sem URL
                    Manifesto = new ManifestoDeploy
                    {
                        Nome = "MeuPacote",
                        Versao = "1.0.0",
                        Develop = false
                    }
                }
            ],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(1.0)
        };

        var resumo = new ResumoDeployConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("pacote1.cmpkg");
        output.ShouldNotContain("URL:");
    }

    private static DeployResultado CriarDeployResultadoSucesso()
        => new()
        {
            ArquivosEnviados =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote1.cmpkg",
                    NomeArquivoS3 = "pacote1.cmpkg",
                    UrlS3 = "https://s3.amazonaws.com/bucket/pacote1.cmpkg",
                    Manifesto = new ManifestoDeploy
                    {
                        Nome = "MeuPacote",
                        Versao = "1.0.0",
                        Develop = false
                    }
                },
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote2.cmpkg",
                    NomeArquivoS3 = "pacote2.cmpkg",
                    UrlS3 = "https://s3.amazonaws.com/bucket/pacote2.cmpkg",
                    Manifesto = new ManifestoDeploy
                    {
                        Nome = "OutroPacote",
                        Versao = "2.1.0",
                        Develop = true
                    }
                }
            ],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(5.2)
        };

    private static DeployResultado CriarDeployResultadoComFalhas()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados = [],
            ArquivosFalharam =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote2.cmpkg",
                    NomeArquivoS3 = "pacote2.cmpkg",
                    MensagemErro = "Falha no upload para S3"
                }
            ],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(3.1)
        };

    private static DeployResultado CriarDeployResultadoComIgnorados()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote3.cmpkg",
                    NomeArquivoS3 = "pacote3.cmpkg",
                    MensagemErro = "Arquivo já existe no S3"
                }
            ],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(1.5)
        };

    private static DeployResultado CriarDeployResultadoSimulado()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = true,
            TempoExecucao = TimeSpan.FromSeconds(0.1)
        };

    private static DeployResultado CriarDeployResultadoVazio()
        => new()
        {
            ArquivosEnviados = [],
            ArquivosIgnorados = [],
            ArquivosFalharam = [],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(0.1)
        };
}
