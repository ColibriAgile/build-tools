using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using BuildTools.Commands;
using BuildTools.Models;
using BuildTools.Services;
using Spectre.Console.Testing;

namespace BuildTools.Testes.Commands;

using NSubstitute.ExceptionExtensions;

/// <summary>
/// Testes para a classe <see cref="NotificarMarketCommand"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NotificarMarketCommandTestes
{
    private const string PASTA_TESTE = @"C:\teste\pasta";
    private const string AMBIENTE_DESENVOLVIMENTO = "desenvolvimento";
    private const string URL_MARKETPLACE_CUSTOM = "https://custom.marketplace.com";
    private const string URL_MARKETPLACE_PADRAO = "https://www.mycolibri.com.br";
    private const string NOME_PACOTE = "TestePacote";
    private const string VERSAO_PACOTE = "1.0.0";

    private readonly IMarketplaceService _marketplaceService = Substitute.For<IMarketplaceService>();
    private readonly IManifestoService _manifestoService = Substitute.For<IManifestoService>();
    private readonly TestConsole _console = new();
    private readonly NotificarMarketCommand _command;

    private readonly Option<bool> _silenciosoOption = new(["--silencioso", "-sl"]) { IsRequired = false };
    private readonly Option<bool> _semCorOption = new(["--sem-cor", "-sc"]) { IsRequired = false };
    private readonly Option<string> _ambienteOption = new(["--ambiente", "-a"]) { IsRequired = false };

    public NotificarMarketCommandTestes()
    {
        _ambienteOption.SetDefaultValue(AMBIENTE_DESENVOLVIMENTO);

        _command = new NotificarMarketCommand
        (
            _silenciosoOption,
            _semCorOption,
            _ambienteOption,
            _marketplaceService,
            _manifestoService,
            _console
        );
    }

    [Fact]
    public async Task Handle_ComParametrosValidos_DeveNotificarMarketplace()
    {
        // Arrange
        var manifesto = CriarManifesto();
        _manifestoService.LerManifestoDeployAsync(PASTA_TESTE).Returns(manifesto);
        _marketplaceService.ObterUrlMarketplace(AMBIENTE_DESENVOLVIMENTO, null).Returns(URL_MARKETPLACE_PADRAO);
        _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_PADRAO, manifesto).Returns(true);

        var rootCommand = new RootCommand
        {
            _command
        };

        // Act
        var result = await rootCommand.InvokeAsync($"notificar-market {PASTA_TESTE}");

        // Assert
        result.ShouldBe(0);
        await _manifestoService.Received(1).LerManifestoDeployAsync(PASTA_TESTE);
        _marketplaceService.Received(1).ObterUrlMarketplace(AMBIENTE_DESENVOLVIMENTO, null);
        await _marketplaceService.Received(1).NotificarPacoteAsync(URL_MARKETPLACE_PADRAO, manifesto);
    }

    [Fact]
    public async Task Handle_ComUrlMarketplaceCustomizada_DeveUsarUrlCustomizada()
    {
        // Arrange
        var manifesto = CriarManifesto();
        _manifestoService.LerManifestoDeployAsync(PASTA_TESTE).Returns(manifesto);
        _marketplaceService.ObterUrlMarketplace(AMBIENTE_DESENVOLVIMENTO, URL_MARKETPLACE_CUSTOM).Returns(URL_MARKETPLACE_CUSTOM);
        _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_CUSTOM, manifesto).Returns(true);

        var rootCommand = new RootCommand
        {
            _command
        };

        // Act
        var result = await rootCommand.InvokeAsync($"notificar-market {PASTA_TESTE} --mkt-url {URL_MARKETPLACE_CUSTOM}");

        // Assert
        result.ShouldBe(0);
        _marketplaceService.Received(1).ObterUrlMarketplace(AMBIENTE_DESENVOLVIMENTO, URL_MARKETPLACE_CUSTOM);
        await _marketplaceService.Received(1).NotificarPacoteAsync(URL_MARKETPLACE_CUSTOM, manifesto);
    }

    [Fact]
    public async Task Handle_ComAmbienteEspecifico_DeveUsarAmbiente()
    {
        // Arrange
        const string AMBIENTE_HOMOLOGACAO = "homologacao";
        var manifesto = CriarManifesto();
        _manifestoService.LerManifestoDeployAsync(PASTA_TESTE).Returns(manifesto);
        _marketplaceService.ObterUrlMarketplace(AMBIENTE_HOMOLOGACAO, null).Returns("https://mycolibri-homolog.ciasc.gov.br");
        _marketplaceService.NotificarPacoteAsync(Arg.Any<string>(), manifesto).Returns(true);

        var rootCommand = new RootCommand
        {
            _command
        };

        // Act
        var result = await rootCommand.InvokeAsync($"notificar-market {PASTA_TESTE} --ambiente {AMBIENTE_HOMOLOGACAO}");

        // Assert
        result.ShouldBe(0);
        _marketplaceService.Received(1).ObterUrlMarketplace(AMBIENTE_HOMOLOGACAO, null);
    }

    [Fact]
    public async Task Handle_ModoSilencioso_NaoDeveExibirMensagens()
    {
        // Arrange
        var manifesto = CriarManifesto();
        _manifestoService.LerManifestoDeployAsync(PASTA_TESTE).Returns(manifesto);
        _marketplaceService.ObterUrlMarketplace(AMBIENTE_DESENVOLVIMENTO, null).Returns(URL_MARKETPLACE_PADRAO);
        _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_PADRAO, manifesto).Returns(true);

        var rootCommand = new RootCommand
        {
            _command
        };

        // Act
        var result = await rootCommand.InvokeAsync($"notificar-market {PASTA_TESTE} --silencioso");

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ModoSemCor_DeveDesabilitarAnsi()
    {
        // Arrange
        var manifesto = CriarManifesto();
        _manifestoService.LerManifestoDeployAsync(PASTA_TESTE).Returns(manifesto);
        _marketplaceService.ObterUrlMarketplace(AMBIENTE_DESENVOLVIMENTO, null).Returns(URL_MARKETPLACE_PADRAO);
        _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_PADRAO, manifesto).Returns(true);

        var rootCommand = new RootCommand
        {
            _command
        };

        // Act
        var result = await rootCommand.InvokeAsync(["notificar-market", PASTA_TESTE, "--sem-cor"]);

        // Assert
        result.ShouldBe(0);
        _console.Profile.Capabilities.Ansi.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_FalhaNaNotificacao_DeveExibirMensagemDeErro()
    {
        // Arrange
        var manifesto = CriarManifesto();
        _manifestoService.LerManifestoDeployAsync(PASTA_TESTE).Returns(manifesto);
        _marketplaceService.ObterUrlMarketplace(AMBIENTE_DESENVOLVIMENTO, null).Returns(URL_MARKETPLACE_PADRAO);
        _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_PADRAO, manifesto).Returns(false);

        var rootCommand = new RootCommand
        {
            _command
        };

        // Act
        var result = await rootCommand.InvokeAsync($"notificar-market {PASTA_TESTE}");

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("ERROR");
        _console.Output.ShouldContain("Falha na notificação do marketplace");
    }

    [Fact]
    public async Task Handle_ExcecaoNaLeituraManifesto_DeveLancarExcecao()
    {
        // Arrange
        _manifestoService.LerManifestoDeployAsync(PASTA_TESTE)
            .ThrowsAsync(new FileNotFoundException("Arquivo não encontrado"));

        var rootCommand = new RootCommand
        {
            _command
        };

        // Act & Assert
        var result = await rootCommand.InvokeAsync($"notificar-market {PASTA_TESTE}");
        result.ShouldNotBe(0);
        _console.Output.ShouldContain("ERROR");
        _console.Output.ShouldContain("Arquivo não encontrado");
    }

    private static ManifestoDeploy CriarManifesto()
        => new()
        {
            Nome = NOME_PACOTE,
            Versao = VERSAO_PACOTE,
            Develop = false,
            SiglaEmpresa = "EMP",
            DadosCompletos = new Dictionary<string, object>
            {
                ["nome"] = NOME_PACOTE,
                ["versao"] = VERSAO_PACOTE,
                ["develop"] = false,
                ["siglaEmpresa"] = "EMP"
            }
        };
}
