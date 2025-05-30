using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using BuildTools.Commands;
using BuildTools.Models;
using BuildTools.Services;
using BuildTools.Validation;
using Spectre.Console.Testing;

namespace BuildTools.Testes.Commands;

[ExcludeFromCodeCoverage]
public sealed class DeployCommandTestes
{
    private readonly TestConsole _console = new();
    private readonly IDeployService _deployService = Substitute.For<IDeployService>();
    private readonly Option<string> _resumoOption = new(aliases: ["--resumo", "-r"]);
    private readonly RootCommand _rootCommand = new("Colibri BuildTools - Deploy de soluções");
    private readonly Option<bool> _semCorOption = new(aliases: ["--sem-cor", "-sc"]);
    private readonly Option<bool> _silenciosoOption = new(aliases: ["--silencioso", "-s"]);
    private readonly Option<string> _ambienteOption = AmbienteValidator.CriarOpcaoAmbiente();

    public DeployCommandTestes()
    {
        var cmd = new DeployCommand(_silenciosoOption, _semCorOption, _resumoOption, _ambienteOption, _deployService, _console);
        _rootCommand.AddGlobalOption(_resumoOption);
        _rootCommand.AddGlobalOption(_silenciosoOption);
        _rootCommand.AddGlobalOption(_semCorOption);
        _rootCommand.AddCommand(cmd);
    }

    [Fact]
    public async Task InvokeAsync_QuandoAmbienteInvalido_DeveRetornarErro()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE_INVALIDO = "ambiente_invalido";

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente",
                AMBIENTE_INVALIDO
            ]
        );

        // Assert
        result.ShouldNotBe(0);

        // O System.CommandLine pode enviar erros de validação para stderr ou usar outro mecanismo
        // Vamos testar se o deployment service não foi chamado devido ao erro de validação
        await _deployService.DidNotReceive().ExecutarDeployAsync
        (
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>()
        );
    }

    [Fact]
    public async Task InvokeAsync_QuandoAmbientePadrao_DeveUsarDesenvolvimento()
    {
        // Arrange
        const string PASTA = @"C:\pasta";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                "desenvolvimento",
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA
            ]
        );

        // Assert
        result.ShouldBe(0);

        await _deployService
            .Received(1)
            .ExecutarDeployAsync
            (
                PASTA,
                "desenvolvimento",
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            );
    }

    [Theory]
    [InlineData("desenvolvimento")]
    [InlineData("producao")]
    [InlineData("stage")]
    public async Task InvokeAsync_QuandoAmbienteValido_DeveExecutarComSucesso(string ambiente)
    {
        // Arrange
        const string PASTA = @"C:\pasta";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                ambiente,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", ambiente
            ]
        );

        // Assert
        result.ShouldBe(0);

        await _deployService
            .Received(1)
            .ExecutarDeployAsync
            (
                PASTA,
                ambiente,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            );
    }

    [Fact]
    public async Task InvokeAsync_QuandoComFalhas_DeveExibirAvisoFalhas()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoComFalhas();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("WARN");
        _console.Output.ShouldContain("Alguns arquivos falharam");
        _console.Output.ShouldContain("Falhas: 1");
    }

    [Fact]
    public async Task InvokeAsync_QuandoCredenciaisAWS_DevePassarParametrosCorretos()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";
        const string AWS_ACCESS_KEY = "AKIAIOSFODNN7EXAMPLE";
        const string AWS_SECRET_KEY = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        const string AWS_REGION = "us-west-2";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                AWS_ACCESS_KEY,
                AWS_SECRET_KEY,
                AWS_REGION
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--aws-access-key", AWS_ACCESS_KEY,
                "--aws-secret-key", AWS_SECRET_KEY,
                "--aws-region", AWS_REGION
            ]
        );

        // Assert
        result.ShouldBe(0);

        await _deployService
            .Received(1)
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                AWS_ACCESS_KEY,
                AWS_SECRET_KEY,
                AWS_REGION
            );
    }

    [Fact]
    public async Task InvokeAsync_QuandoDeployComSucesso_DeveExibirMensagemDeSucesso()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("SUCCESS");
        _console.Output.ShouldContain("Deploy concluído");
        _console.Output.ShouldContain("Arquivos processados: 2");
        _console.Output.ShouldContain("Enviados: 2");
    }

    [Fact]
    public async Task InvokeAsync_QuandoDeployFalha_DeveExibirMensagemDeErro()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";
        const string MENSAGEM_ERRO = "Falha ao executar deploy";

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(Task.FromException<DeployResultado>(new Exception(MENSAGEM_ERRO)));

        // Act
        var res = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE
            ]
        );

        // Assert
        res.ShouldBe(1);
        _console.Output.ShouldContain("ERROR");
        _console.Output.ShouldContain(MENSAGEM_ERRO);
    }

    [Fact]
    public async Task InvokeAsync_QuandoForcar_DevePassarParametroCorreto()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                true,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--forcar"
            ]
        );

        // Assert
        result.ShouldBe(0);

        await _deployService
            .Received(1)
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                true,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            );
    }

    [Fact]
    public async Task InvokeAsync_QuandoMarketplaceUrl_DevePassarParametroCorreto()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";
        const string MARKETPLACE_URL = "https://marketplace.exemplo.com";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                MARKETPLACE_URL,
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--mkt-url", MARKETPLACE_URL
            ]
        );

        // Assert
        result.ShouldBe(0);

        await _deployService
            .Received(1)
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                MARKETPLACE_URL,
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            );
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoConsole_DeveExibirResumoConsole()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--resumo", "console"
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("Arquivos processados: 2");
    }

    [Fact]
    public async Task InvokeAsync_QuandoResumoMarkdown_DeveExibirResumoMarkdown()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--resumo", "markdown"
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("# Relatório de Deploy");
    }

    [Fact]
    public async Task InvokeAsync_QuandoSemCor_DeveDesabilitarAnsi()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--sem-cor"
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Profile.Capabilities.Ansi.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_QuandoSilencioso_NaoDeveExibirMensagens()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--silencioso"
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldNotContain("INFO");
        _console.Output.ShouldNotContain("SUCCESS");
    }

    [Fact]
    public async Task InvokeAsync_QuandoSimulado_DevePassarParametroCorreto()
    {
        // Arrange
        const string PASTA = @"C:\pasta";
        const string AMBIENTE = "desenvolvimento";

        var resultado = CriarDeployResultadoSucesso();

        _deployService
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                true,
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            )
            .Returns(resultado);

        // Act
        var result = await _rootCommand.InvokeAsync
        (
            [
                "deploy",
                PASTA,
                "--ambiente", AMBIENTE,
                "--simulado"
            ]
        );

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("(SIMULADO)");

        await _deployService
            .Received(1)
            .ExecutarDeployAsync
            (
                PASTA,
                AMBIENTE,
                Arg.Any<string?>(),
                true,
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>()
            );
    }

    private static DeployResultado CriarDeployResultadoComFalhas()
        => new()
        {
            ArquivosEnviados =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote1.cmpkg",
                    NomeArquivoS3 = "pacote1.cmpkg",
                    UrlS3 = "https://s3.amazonaws.com/bucket/pacote1.cmpkg"
                }
            ],
            ArquivosIgnorados = [],
            ArquivosFalharam =
            [
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote2.cmpkg",
                    MensagemErro = "Falha no upload"
                }
            ],
            Ambiente = "desenvolvimento",
            UrlMarketplace = "https://marketplace.exemplo.com",
            Simulado = false,
            TempoExecucao = TimeSpan.FromSeconds(3.1)
        };

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
                    Manifesto = new()
                    {
                        Nome = "Pacote 1",
                        Versao = "1.0.0",
                        Develop = true,
                        SiglaEmpresa = "EMPRESA1"
                    }
                },
                new DeployArquivo
                {
                    CaminhoArquivo = @"C:\pasta\pacote2.cmpkg",
                    NomeArquivoS3 = "pacote2.cmpkg",
                    UrlS3 = "https://s3.amazonaws.com/bucket/pacote2.cmpkg",
                    Manifesto = new()
                    {
                        Nome = "Pacote 2",
                        Versao = "2.0.0",
                        Develop = true,
                        SiglaEmpresa = "EMPRESA2"
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
}