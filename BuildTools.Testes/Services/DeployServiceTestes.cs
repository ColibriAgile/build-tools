using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BuildTools.Models;
using BuildTools.Services;
using NSubstitute.ExceptionExtensions;
using Spectre.Console.Testing;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes para a classe <see cref="DeployService"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeployServiceTestes
{
    private const string AMBIENTE_DESENVOLVIMENTO = "desenvolvimento";
    private const string AMBIENTE_PRODUCAO = "producao";
    private const string AMBIENTE_STAGE = "stage";
    private const string AMBIENTE_INVALIDO = "ambiente-invalido";
    private const string PASTA_TESTES = @"C:\temp\deploy";
    private const string ARQUIVO_MANIFESTO = "manifesto.dat";
    private const string ARQUIVO_CMPKG = "pacote.cmpkg";
    private const string AWS_ACCESS_KEY = "test-access-key";
    private const string AWS_SECRET_KEY = "test-secret-key";
    private const string AWS_REGION = "us-east-1";
    private const string URL_MARKETPLACE_CUSTOM = "https://custom-marketplace.com";
    private const string BUCKET_NAME = "ncr-colibri";

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IS3Service _s3Service = Substitute.For<IS3Service>();
    private readonly IMarketplaceService _marketplaceService = Substitute.For<IMarketplaceService>();
    private readonly TestConsole _console = new();
    private readonly DeployService _deployService;

    public DeployServiceTestes()
        => _deployService = new DeployService(_fileSystem, _s3Service, _marketplaceService, _console);

    [Fact]
    public async Task ExecutarDeployAsync_AmbienteValido_DeveRetornarResultadoComSucesso()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false, "EMP");

        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>())
            .Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        _marketplaceService.NotificarPacoteAsync(Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns(true);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);
        Environment.SetEnvironmentVariable("AWS_REGION", AWS_REGION);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Ambiente.ShouldBe(AMBIENTE_DESENVOLVIMENTO);
        resultado.Simulado.ShouldBeFalse();
        resultado.ArquivosEnviados.ShouldHaveSingleItem();
        resultado.ArquivosFalharam.ShouldBeEmpty();
        resultado.ArquivosIgnorados.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecutarDeployAsync_AmbienteInvalido_DeveLancarArgumentException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>
        (
            () => _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_INVALIDO)
        );

        exception.Message.ShouldContain($"Ambiente '{AMBIENTE_INVALIDO}' inválido");
    }

    [Theory]
    [InlineData(AMBIENTE_DESENVOLVIMENTO)]
    [InlineData(AMBIENTE_PRODUCAO)]
    [InlineData(AMBIENTE_STAGE)]
    public async Task ExecutarDeployAsync_AmbientesValidos_NaoDeveLancarExcecao(string ambiente)
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);

        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>())
            .Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act & Assert
        await Should.NotThrowAsync(() => _deployService.ExecutarDeployAsync(PASTA_TESTES, ambiente, simulado: true));
    }

    [Fact]
    public async Task ExecutarDeployAsync_SemCredenciaisAws_DeveLancarInvalidOperationException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>
        (
            () => _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO)
        );

        exception.Message.ShouldContain("AWS Access Key não informada");
    }

    [Fact]
    public async Task ExecutarDeployAsync_ComCredenciaisParametros_DeveUsarCredenciaisParametros()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);

        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>())
            .Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);

        // Act
        await _deployService.ExecutarDeployAsync
        (
            PASTA_TESTES,
            AMBIENTE_DESENVOLVIMENTO,
            simulado: true,
            awsAccessKey: AWS_ACCESS_KEY,
            awsSecretKey: AWS_SECRET_KEY,
            awsRegion: AWS_REGION
        );

        // Assert
        // Verifica se as credenciais foram configuradas no S3Service
        _s3Service.DidNotReceive().ConfigurarCredenciais(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecutarDeployAsync_ModoSimulado_NaoDeveConfigurarCredenciaisS3()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);

        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>())
            .Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO, simulado: true);

        // Assert
        _s3Service.DidNotReceive().ConfigurarCredenciais(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecutarDeployAsync_ComUrlMarketplaceCustomizada_DeveUsarUrlCustomizada()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);

        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>())
            .Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(
            PASTA_TESTES,
            AMBIENTE_DESENVOLVIMENTO,
            URL_MARKETPLACE_CUSTOM,
            simulado: true);

        // Assert
        resultado.UrlMarketplace.ShouldBe(URL_MARKETPLACE_CUSTOM);
    }

    [Fact]
    public async Task ExecutarDeployAsync_ComVariavelTEST_DeveUsarUrlLocalhost()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);

        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>())
            .Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);
        Environment.SetEnvironmentVariable("TEST", "true");

        try
        {
            // Act
            var resultado = await _deployService.ExecutarDeployAsync
            (
                PASTA_TESTES,
                AMBIENTE_DESENVOLVIMENTO,
                simulado: true
            );

            // Assert
            resultado.UrlMarketplace.ShouldBe("http://localhost:8888");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST", null);
        }
    }

    [Theory]
    [InlineData(AMBIENTE_STAGE, "https://qa-marketplace.ncrcolibri.com.br")]
    [InlineData(AMBIENTE_PRODUCAO, "https://marketplace.ncrcolibri.com.br")]
    [InlineData(AMBIENTE_DESENVOLVIMENTO, "https://qa-marketplace.ncrcolibri.com.br")]
    public async Task ExecutarDeployAsync_AmbientesEspecificos_DeveUsarUrlCorreta(string ambiente, string urlEsperada)
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);

        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>())
            .Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);
        Environment.SetEnvironmentVariable("TEST", null);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, ambiente, simulado: true);

        // Assert
        resultado.UrlMarketplace.ShouldBe(urlEsperada);
    }

    [Fact]
    public async Task ExecutarDeployAsync_PastaNaoExiste_DeveLancarDirectoryNotFoundException()
    {
        // Arrange
        _fileSystem.Directory.Exists(Arg.Any<string>()).Returns(false);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act & Assert
        var exception = await Should.ThrowAsync<DirectoryNotFoundException>
        (
            () => _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO)
        );

        exception.Message.ShouldContain($"Pasta não encontrada: {PASTA_TESTES}");
    }

    [Fact]
    public async Task ExecutarDeployAsync_SemArquivosManifesto_DeveRetornarResultadoVazio()
    {
        // Arrange
        _fileSystem.Directory.Exists(PASTA_TESTES).Returns(true);
        _fileSystem.Directory.GetFiles(PASTA_TESTES, "*.dat").Returns([]);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

        // Assert
        resultado.ArquivosEnviados.ShouldBeEmpty();
        resultado.ArquivosFalharam.ShouldBeEmpty();
        resultado.ArquivosIgnorados.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecutarDeployAsync_ArquivoCmpkgNaoExiste_DeveLogarWarning()
    {
        // Arrange
        var manifestoPath = Path.Combine(PASTA_TESTES, ARQUIVO_MANIFESTO);

        _fileSystem.Directory.Exists(PASTA_TESTES).Returns(true);
        _fileSystem.Directory.GetFiles(PASTA_TESTES, "*.dat").Returns([manifestoPath]);
        _fileSystem.Directory.GetFiles(PASTA_TESTES, "*.cmpkg").Returns([]);
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
        _fileSystem.Path.GetFileName(manifestoPath).Returns(ARQUIVO_MANIFESTO);

        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(manifestoPath).Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

        // Assert
        resultado.ArquivosEnviados.ShouldBeEmpty();
        _console.Output.ShouldContain("WARN");
        _console.Output.ShouldContain("Arquivo .cmpkg não encontrado");
    }

    [Fact]
    public async Task ExecutarDeployAsync_ArquivoJaExisteEForcarFalse_DeveIgnorarArquivo()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO, forcar: false);

        // Assert
        resultado.ArquivosIgnorados.ShouldHaveSingleItem();
        resultado.ArquivosEnviados.ShouldBeEmpty();
        resultado.ArquivosIgnorados.First().MensagemErro.ShouldBe("Arquivo já existe no S3");
    }

    [Fact]
    public async Task ExecutarDeployAsync_ArquivoJaExisteEForcarTrue_DeveEnviarArquivo()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        _marketplaceService.NotificarPacoteAsync(Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns(true);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO, forcar: true);

        // Assert
        resultado.ArquivosEnviados.ShouldHaveSingleItem();
        resultado.ArquivosIgnorados.ShouldBeEmpty();
        await _s3Service.DidNotReceive().ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecutarDeployAsync_FalhaUploadS3_DeveAdicionarArquivoFalharam()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .ThrowsAsync(new Exception("Erro no S3"));

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

        // Assert
        resultado.ArquivosFalharam.ShouldHaveSingleItem();
        resultado.ArquivosEnviados.ShouldBeEmpty();
        resultado.ArquivosFalharam[0].MensagemErro.ShouldBe("Erro no S3");
    }

    [Fact]
    public async Task ExecutarDeployAsync_FalhaNotificacaoMarketplace_DeveLogarWarning()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        _marketplaceService.NotificarPacoteAsync(Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns(false);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

        // Assert
        resultado.ArquivosEnviados.ShouldHaveSingleItem();
        _console.Output.ShouldContain("WARN");
        _console.Output.ShouldContain("Falha ao notificar o marketplace");
    }

    [Theory]
    [InlineData(true, "packages-dev")]
    [InlineData(false, "packages-dev")]
    public async Task ExecutarDeployAsync_AmbienteDesenvolvimento_DeveUsarPrefixoCorreto(bool develop, string prefixoEsperado)
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", develop);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);
        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

        // Assert
        await _s3Service.Received(1).FazerUploadAsync
        (
            BUCKET_NAME,
            Arg.Is<string>(chave => chave.StartsWith(prefixoEsperado)),
            Arg.Any<string>(),
            Arg.Any<ManifestoDeploy>()
        );
    }

    [Fact]
    public async Task ExecutarDeployAsync_AmbienteProducao_DeveUsarPrefixoProducao()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_PRODUCAO);

        // Assert
        await _s3Service.Received(1).FazerUploadAsync
        (
            BUCKET_NAME,
            Arg.Is<string>(static chave => chave.StartsWith("packages")),
            Arg.Any<string>(),
            Arg.Any<ManifestoDeploy>()
        );
    }

    [Fact]
    public async Task ExecutarDeployAsync_AmbienteStage_DeveUsarPrefixoStage()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_STAGE);

        // Assert
        await _s3Service.Received(1).FazerUploadAsync
        (
            BUCKET_NAME,
            Arg.Is<string>(static chave => chave.StartsWith("packages-stage")),
            Arg.Any<string>(),
            Arg.Any<ManifestoDeploy>()
        );
    }

    [Fact]
    public async Task ExecutarDeployAsync_ComVariavelSTAGE_DeveUsarPrefixoStage()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);
        Environment.SetEnvironmentVariable("STAGE", "true");

        try
        {
            // Act
            await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

            // Assert
            await _s3Service.Received(1).FazerUploadAsync
            (
                BUCKET_NAME,
                Arg.Is<string>(static chave => chave.StartsWith("packages-stage")),
                Arg.Any<string>(),
                Arg.Any<ManifestoDeploy>()
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable("STAGE", null);
        }
    }

    [Fact]
    public async Task ExecutarDeployAsync_PacoteDevelop_DeveUsarPrefixoDesenvolvimento()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", true);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        _s3Service.ArquivoExisteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        _s3Service.FazerUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ManifestoDeploy>())
            .Returns("https://s3.amazonaws.com/test-url");

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_PRODUCAO);

        // Assert
        await _s3Service.Received(1).FazerUploadAsync
        (
            BUCKET_NAME,
            Arg.Is<string>(static chave => chave.StartsWith("packages-dev")),
            Arg.Any<string>(),
            Arg.Any<ManifestoDeploy>()
        );
    }

    [Fact]
    public async Task ExecutarDeployAsync_PacoteComSiglaEmpresa_DeveGerarNomeCorreto()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false, "EMP");
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        var caminhoArquivoCmpkg = Path.Combine(PASTA_TESTES, "EMP-TestePacote_1_0_0.cmpkg");
        _fileSystem.Path.Combine(PASTA_TESTES, "EMP-TestePacote_1_0_0.cmpkg").Returns(caminhoArquivoCmpkg);
        _fileSystem.File.Exists(caminhoArquivoCmpkg).Returns(true);
        _fileSystem.Path.GetFileName(caminhoArquivoCmpkg).Returns("EMP-TestePacote_1_0_0.cmpkg");

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO, simulado: true);

        // Assert
        resultado.ArquivosEnviados.ShouldHaveSingleItem();
        resultado.ArquivosEnviados[0].NomeArquivoS3.ShouldBe("EMP-TestePacote_1_0_0.cmpkg");
    }

    [Fact]
    public async Task ExecutarDeployAsync_PacoteSemSiglaEmpresa_DeveGerarNomeCorreto()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        var caminhoArquivoCmpkg = Path.Combine(PASTA_TESTES, "TestePacote_1_0_0.cmpkg");
        _fileSystem.Path.Combine(PASTA_TESTES, "TestePacote_1_0_0.cmpkg").Returns(caminhoArquivoCmpkg);
        _fileSystem.File.Exists(caminhoArquivoCmpkg).Returns(true);
        _fileSystem.Path.GetFileName(caminhoArquivoCmpkg).Returns("TestePacote_1_0_0.cmpkg");

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO, simulado: true);

        // Assert
        resultado.ArquivosEnviados.ShouldHaveSingleItem();
        resultado.ArquivosEnviados[0].NomeArquivoS3.ShouldBe("TestePacote_1_0_0.cmpkg");
    }

    [Fact]
    public async Task ExecutarDeployAsync_ErroDeserializacaoManifesto_DeveLogarErro()
    {
        // Arrange
        var manifestoPath = Path.Combine(PASTA_TESTES, ARQUIVO_MANIFESTO);

        _fileSystem.Directory.Exists(PASTA_TESTES).Returns(true);
        _fileSystem.Directory.GetFiles(PASTA_TESTES, "*.dat").Returns([manifestoPath]);
        _fileSystem.File.ReadAllTextAsync(manifestoPath).Returns("json-invalido");
        _fileSystem.Path.GetFileName(manifestoPath).Returns(ARQUIVO_MANIFESTO);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO);

        // Assert
        resultado.ArquivosEnviados.ShouldBeEmpty();
        _console.Output.ShouldContain("ERROR");
        _console.Output.ShouldContain("Erro ao processar manifesto");
    }

    [Fact]
    public async Task ExecutarDeployAsync_DeveRegistrarTempoExecucao()
    {
        // Arrange
        ConfigurarFileSystemComArquivos();
        var manifestoJson = CriarManifestoJson("TestePacote", "1.0.0", false);
        _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(manifestoJson);

        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AWS_ACCESS_KEY);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", AWS_SECRET_KEY);

        // Act
        var resultado = await _deployService.ExecutarDeployAsync(PASTA_TESTES, AMBIENTE_DESENVOLVIMENTO, simulado: true);

        // Assert
        resultado.TempoExecucao.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    private void ConfigurarFileSystemComArquivos()
    {
        var manifestoPath = Path.Combine(PASTA_TESTES, ARQUIVO_MANIFESTO);
        var cmpkgPath = Path.Combine(PASTA_TESTES, ARQUIVO_CMPKG);

        _fileSystem.Directory.Exists(PASTA_TESTES).Returns(true);
        _fileSystem.Directory.GetFiles(PASTA_TESTES, "*.dat").Returns([manifestoPath]);
        _fileSystem.Directory.GetFiles(PASTA_TESTES, "*.cmpkg").Returns([cmpkgPath]);
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(static x => Path.Combine(x[0].ToString()!, x[1].ToString()!));
        _fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(static x => Path.GetFileName(x[0].ToString()));
    }

    private static string CriarManifestoJson(string nome, string versao, bool develop, string? siglaEmpresa = null)
    {
        var manifesto = new Dictionary<string, object>
        {
            ["nome"] = nome,
            ["versao"] = versao,
            ["develop"] = develop
        };

        if (!string.IsNullOrEmpty(siglaEmpresa))
            manifesto["siglaEmpresa"] = siglaEmpresa;

        return JsonSerializer.Serialize(manifesto);
    }
}
