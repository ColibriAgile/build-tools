using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using BuildTools.Models;
using BuildTools.Services;
using BuildTools.Testes.Helpers;
using Spectre.Console.Testing;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes para a classe <see cref="MarketplaceService"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MarketplaceServiceTestes : IDisposable
{
    private const string URL_MARKETPLACE_BASE = "https://marketplace.test.com";
    private const string URL_MARKETPLACE_COM_BARRA = "https://marketplace.test.com/";
    private const string TOKEN_JWT = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test";
    private const string NOME_PACOTE = "TestePacote";
    private const string VERSAO_PACOTE = "1.0.0";
    private const string SIGLA_EMPRESA = "EMP";
    private const string ENDPOINT_NOTIFICAR = "/api/secure/pacote/sync/";

    private readonly HttpClient _httpClient;
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly MarketplaceService _marketplaceService;
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly TestConsole _console = new();

    public MarketplaceServiceTestes()
    {
        _httpMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_httpMessageHandler);
        _marketplaceService = new MarketplaceService(_httpClient, _jwtService, _console);
    }

    [Fact]
    public async Task NotificarPacoteAsync_RequisicaoComSucesso_DeveRetornarTrue()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false, SIGLA_EMPRESA);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        var resultado = await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        resultado.ShouldBeTrue();
    }

    [Fact]
    public async Task NotificarPacoteAsync_RequisicaoComSucesso_DeveEnviarPayloadCorreto()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false, SIGLA_EMPRESA);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        var request = _httpMessageHandler.LastRequest;

        request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri?.ToString().ShouldBe($"{URL_MARKETPLACE_BASE}{ENDPOINT_NOTIFICAR}");

        var content = await request.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        payload.ShouldNotBeNull();
        payload.ShouldContainKey("nome");
        payload.ShouldContainKey("versao");
        payload.ShouldContainKey("manifesto");

        var nomeElement = (JsonElement)payload["nome"];
        var versaoElement = (JsonElement)payload["versao"];

        nomeElement.GetString().ShouldBe(NOME_PACOTE);
        versaoElement.GetString().ShouldBe(VERSAO_PACOTE);
    }

    [Fact]
    public async Task NotificarPacoteAsync_RequisicaoComSucesso_DeveEnviarHeadersCorretos()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        var request = _httpMessageHandler.LastRequest;

        request.ShouldNotBeNull();
        request.Headers.Authorization?.Scheme.ShouldBe("Bearer");
        request.Headers.Authorization?.Parameter.ShouldBe(TOKEN_JWT);
        request.Content?.Headers.ContentType?.MediaType.ShouldBe("application/json");
        request.Content?.Headers.ContentType?.CharSet.ShouldBe("utf-8");
    }

    [Fact]
    public async Task NotificarPacoteAsync_UrlComBarraFinal_DeveRemoverBarraDuplicada()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_COM_BARRA, manifesto);

        // Assert
        var request = _httpMessageHandler.LastRequest;

        request.ShouldNotBeNull();
        request.RequestUri?.ToString().ShouldBe($"{URL_MARKETPLACE_BASE}{ENDPOINT_NOTIFICAR}");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.BadGateway, "Bad Gateway")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task NotificarPacoteAsync_RespostaComErro_DeveRetornarFalse(HttpStatusCode statusCode, string reason)
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(statusCode, "Error");

        // Act
        var resultado = await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        resultado.ShouldBeFalse();
        _console.Output.ShouldContain("[ERROR]Erro ao notificar market: " + (int)statusCode + " - " + reason);
    }

    [Fact]
    public async Task NotificarPacoteAsync_ExcecaoNaRequisicao_DeveRetornarFalse()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetException(new HttpRequestException("Erro de rede"));

        // Act
        var resultado = await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        resultado.ShouldBeFalse();
    }

    [Fact]
    public async Task NotificarPacoteAsync_ExcecaoNoJwtService_DeveRetornarFalse()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _jwtService.When(x => x.GerarToken()).Do(x => throw new InvalidOperationException("Erro JWT"));

        // Act
        var resultado = await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        resultado.ShouldBeFalse();
    }

    [Fact]
    public async Task NotificarPacoteAsync_ManifestoComDadosCompletos_DeveIncluirTodosOsDados()
    {
        // Arrange
        var dadosCompletos = new Dictionary<string, object>
        {
            ["nome"] = NOME_PACOTE,
            ["versao"] = VERSAO_PACOTE,
            ["autor"] = "Autor Teste",
            ["descricao"] = "Descrição do pacote",
            ["categoria"] = "Categoria Teste"
        };

        var manifesto = new ManifestoDeploy
        {
            Nome = NOME_PACOTE,
            Versao = VERSAO_PACOTE,
            Develop = false,
            SiglaEmpresa = SIGLA_EMPRESA,
            DadosCompletos = dadosCompletos
        };

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        var request = _httpMessageHandler.LastRequest;
        var content = await request!.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        payload.ShouldNotBeNull();
        payload.ShouldContainKey("manifesto");

        var manifestoElement = (JsonElement)payload["manifesto"];
        var manifestoDict = JsonSerializer.Deserialize<Dictionary<string, object>>(manifestoElement.GetRawText());

        manifestoDict.ShouldNotBeNull();
        manifestoDict.ShouldContainKey("autor");
        manifestoDict.ShouldContainKey("descricao");
        manifestoDict.ShouldContainKey("categoria");
    }

    [Fact]
    public async Task NotificarPacoteAsync_ManifestoSemSiglaEmpresa_DeveProcessarNormalmente()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, true, null);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        var resultado = await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        resultado.ShouldBeTrue();

        var request = _httpMessageHandler.LastRequest;
        var content = await request!.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        payload.ShouldNotBeNull();
        payload.ShouldContainKey("nome");
        payload.ShouldContainKey("versao");
        payload.ShouldContainKey("manifesto");
    }

    [Fact]
    public async Task NotificarPacoteAsync_MultiplasChamadas_DeveLimparHeadersEntreChamadas()
    {
        // Arrange
        var manifesto1 = CriarManifesto("Pacote1", "1.0.0", false);
        var manifesto2 = CriarManifesto("Pacote2", "2.0.0", false);

        _jwtService.GerarToken().Returns(TOKEN_JWT);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto1);
        await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto2);

        // Assert
        var request = _httpMessageHandler.LastRequest;

        request.ShouldNotBeNull();
        request.Headers.Authorization?.Parameter.ShouldBe(TOKEN_JWT);

        // Verifica se não há headers duplicados ou acumulados
        request.Headers.GetValues("TOKEN").ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task NotificarPacoteAsync_UrlMarketplaceInvalida_DeveRetornarFalse(string? urlInvalida)
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _jwtService.GerarToken().Returns(TOKEN_JWT);

        // Act
        var resultado = await _marketplaceService.NotificarPacoteAsync(urlInvalida!, manifesto);

        // Assert
        resultado.ShouldBeFalse();
    }

    [Fact]
    public async Task NotificarPacoteAsync_TokenVazio_DeveEnviarHeaderVazio()
    {
        // Arrange
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _jwtService.GerarToken().Returns(string.Empty);
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, "Success");

        // Act
        await _marketplaceService.NotificarPacoteAsync(URL_MARKETPLACE_BASE, manifesto);

        // Assert
        var request = _httpMessageHandler.LastRequest;

        request.ShouldNotBeNull();
        request.Headers.Authorization?.Parameter.ShouldBeNullOrEmpty();
    }

    [Theory]
    [InlineData("desenvolvimento", null, "https://qa-marketplace.ncrcolibri.com.br")]
    [InlineData("stage", null, "https://qa-marketplace.ncrcolibri.com.br")]
    [InlineData("producao", null, "https://marketplace.ncrcolibri.com.br")]
    public void ObterUrlMarketplace_AmbienteSemUrlCustomizada_DeveRetornarUrlPadrao(string ambiente, string? urlCustomizada, string urlEsperada)
    {
        // Act
        var resultado = _marketplaceService.ObterUrlMarketplace(ambiente, urlCustomizada);

        // Assert
        resultado.ShouldBe(urlEsperada);
    }

    [Theory]
    [InlineData("desenvolvimento", "https://custom.marketplace.com")]
    [InlineData("homologacao", "https://custom.marketplace.com")]
    [InlineData("producao", "https://custom.marketplace.com")]
    public void ObterUrlMarketplace_AmbienteComUrlCustomizada_DeveRetornarUrlCustomizada(string ambiente, string urlCustomizada)
    {
        // Act
        var resultado = _marketplaceService.ObterUrlMarketplace(ambiente, urlCustomizada);

        // Assert
        resultado.ShouldBe(urlCustomizada);
    }

    [Fact]
    public void ObterUrlMarketplace_AmbienteInvalido_DeveLancarArgumentException()
    {
        // Arrange
        const string AMBIENTE_INVALIDO = "ambiente-inexistente";

        // Act & Assert
        Should.Throw<ArgumentException>(() => _marketplaceService.ObterUrlMarketplace(AMBIENTE_INVALIDO, null))
            .Message.ShouldContain($"Ambiente '{AMBIENTE_INVALIDO}' não é válido");
    }

    private static ManifestoDeploy CriarManifesto(string nome, string versao, bool develop, string? siglaEmpresa = null)
    {
        var dadosCompletos = new Dictionary<string, object>
        {
            ["nome"] = nome,
            ["versao"] = versao,
            ["develop"] = develop
        };

        if (!string.IsNullOrEmpty(siglaEmpresa))
            dadosCompletos["siglaEmpresa"] = siglaEmpresa;

        return new ManifestoDeploy
        {
            Nome = nome,
            Versao = versao,
            Develop = develop,
            SiglaEmpresa = siglaEmpresa,
            DadosCompletos = dadosCompletos
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _httpMessageHandler.Dispose();
    }
}