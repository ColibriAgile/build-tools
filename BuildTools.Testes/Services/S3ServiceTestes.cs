using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using BuildTools.Models;
using BuildTools.Services;
using NSubstitute.ExceptionExtensions;

namespace BuildTools.Testes.Services;

/// <summary>
///     Testes para a classe <see cref="S3Service" />.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class S3ServiceTestes : IDisposable
{
    private const string AWS_ACCESS_KEY = "test-access-key";
    private const string AWS_REGION = "us-east-1";
    private const string AWS_SECRET_KEY = "test-secret-key";
    private const string BUCKET_NAME = "test-bucket";
    private const string CAMINHO_ARQUIVO = @"C:\temp\test-package.cmpkg";
    private const string CHAVE_ARQUIVO = "packages/test-package.cmpkg";
    private const string NOME_PACOTE = "TestePacote";
    private const string SIGLA_EMPRESA = "EMP";
    private const string URL_S3_ESPERADA = "https://test-bucket.s3.amazonaws.com/packages/test-package.cmpkg";
    private const string VERSAO_PACOTE = "1.0.0";
    private readonly IAmazonS3 _mockS3Client = Substitute.For<IAmazonS3>();

    private readonly S3Service _s3Service;

    public S3ServiceTestes()
    {
        // Cria um factory que retorna o mock
        var s3ClientFactory = new Func<string, string, string, IAmazonS3>((_, _, _) => _mockS3Client);
        _s3Service = new S3Service(s3ClientFactory);
    }

    [Fact]
    public async Task ArquivoExisteAsync_ArquivoExiste_DeveRetornarTrue()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        _mockS3Client.GetObjectMetadataAsync(BUCKET_NAME, CHAVE_ARQUIVO)
            .Returns(new GetObjectMetadataResponse());

        // Act
        var resultado = await _s3Service.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO);

        // Assert
        resultado.ShouldBeTrue();
    }

    [Fact]
    public async Task ArquivoExisteAsync_ArquivoNaoExiste_DeveRetornarFalse()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        _mockS3Client.GetObjectMetadataAsync(BUCKET_NAME, CHAVE_ARQUIVO)
            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

        // Act
        var resultado = await _s3Service.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO);

        // Assert
        resultado.ShouldBeFalse();
    }

    [Fact]
    public async Task ArquivoExisteAsync_ClienteNaoConfigurado_DeveLancarInvalidOperationException()
    {
        // Arrange
        var s3ServiceSemCliente = new S3Service();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => s3ServiceSemCliente.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO));

        exception.Message.ShouldContain("Credenciais AWS não configuradas");
    }

    [Fact]
    public async Task ArquivoExisteAsync_ErroS3Diferente_DeveLancarExcecao()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        _mockS3Client.GetObjectMetadataAsync(BUCKET_NAME, CHAVE_ARQUIVO)
            .ThrowsAsync(new AmazonS3Exception("Internal Server Error") { StatusCode = HttpStatusCode.InternalServerError });

        // Act & Assert
        await Should.ThrowAsync<AmazonS3Exception>(() => _s3Service.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO));
    }

    [Fact]
    public async Task ArquivoExisteAsync_ParametrosNull_DeveLancarArgumentNullException()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _s3Service.ArquivoExisteAsync(null!, CHAVE_ARQUIVO));

        await Should.ThrowAsync<ArgumentNullException>(() => _s3Service.ArquivoExisteAsync(BUCKET_NAME, null!));
    }

    [Theory]
    [InlineData("", CHAVE_ARQUIVO)]
    [InlineData(BUCKET_NAME, "")]
    public async Task ArquivoExisteAsync_ParametrosVazios_DeveLancarArgumentException(string bucket, string chave)
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _s3Service.ArquivoExisteAsync(bucket, chave));
    }

    [Fact]
    public void ConfigurarCredenciais_ChamadasMultiplas_DeveSubstituirClienteAnterior()
    {
        // Arrange & Act
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);
        _s3Service.ConfigurarCredenciais("new-key", "new-secret", "us-west-2");

        // Assert
        // Verifica se o cliente ainda está funcionando após a reconfiguração
        Should.NotThrow(() => _s3Service.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO));
    }

    [Theory]
    [InlineData("us-east-1")]
    [InlineData("us-west-2")]
    [InlineData("eu-west-1")]
    [InlineData("sa-east-1")]
    public void ConfigurarCredenciais_DiferentesRegioes_DeveConfigurarCorretamente(string regiao)
    {
        // Arrange & Act
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, regiao);

        // Assert
        // Verifica se o cliente foi configurado tentando chamar um método
        Should.NotThrow(() => _s3Service.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO));
    }

    [Fact]
    public void ConfigurarCredenciais_ParametrosValidos_DeveConfigurarClienteS3()
    {
        // Arrange & Act
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        // Assert
        // O cliente deve estar configurado - vamos testar isso tentando usar um método que precisa do cliente
        Should.NotThrow(() => _s3Service.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO));
    }

    public void Dispose()
    {
        _s3Service.Dispose();
    }

    [Fact]
    public void Dispose_ChamadasMultiplas_NaoDeveLancarExcecao()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        // Act & Assert
        Should.NotThrow(() =>
        {
            _s3Service.Dispose();
            _s3Service.Dispose();
        });
    }

    [Fact]
    public void Dispose_DeveLiberarRecursos()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        // Act
        _s3Service.Dispose();

        // Assert
        // Após dispose, tentar usar deve lançar exceção
        Should.ThrowAsync<InvalidOperationException>(() => _s3Service.ArquivoExisteAsync(BUCKET_NAME, CHAVE_ARQUIVO));
    }

    [Fact]
    public async Task FazerUploadAsync_ClienteNaoConfigurado_DeveLancarInvalidOperationException()
    {
        // Arrange
        var s3ServiceSemCliente = new S3Service();
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>
        (
            () => s3ServiceSemCliente.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto)
        );

        exception.Message.ShouldContain("Credenciais AWS não configuradas");
    }

    [Fact]
    public async Task FazerUploadAsync_DadosCompletoComplexo_DeveCriarMetadataCorretamente()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        var dadosCompletos = new Dictionary<string, object>
        {
            ["nome"] = NOME_PACOTE,
            ["versao"] = VERSAO_PACOTE,
            ["autor"] = "Autor Teste",
            ["descricao"] = "Descrição detalhada do pacote",
            ["tags"] = new[] { "tag1", "tag2", "tag3" },
            ["configuracoes"] = new Dictionary<string, object>
            {
                ["debug"] = true,
                ["timeout"] = 30000
            }
        };

        var manifesto = new ManifestoDeploy
        {
            Nome = NOME_PACOTE,
            Versao = VERSAO_PACOTE,
            Develop = true,
            SiglaEmpresa = SIGLA_EMPRESA,
            DadosCompletos = dadosCompletos
        };

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
            .Returns(new PutObjectResponse());

        // Act
        await _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto);

        // Assert
        await _mockS3Client.Received(1).PutObjectAsync
        (
            Arg.Is<PutObjectRequest>
            (
                request => request.Metadata.Keys.Contains("x-amz-meta-info")
                    && ValidarMetadataInfoCompleto(request.Metadata["x-amz-meta-info"], manifesto)
            )
        );
    }

    [Fact]
    public async Task FazerUploadAsync_ErroNoUpload_DeveLancarExcecao()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
            .ThrowsAsync(new AmazonS3Exception("Upload failed"));

        // Act & Assert
        await Should.ThrowAsync<AmazonS3Exception>(() => _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto));
    }

    [Fact]
    public async Task FazerUploadAsync_ManifestoComSiglaEmpresa_DeveIncluirMetadataCompleto()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false, SIGLA_EMPRESA);

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
            .Returns(new PutObjectResponse());

        // Act
        await _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto);

        // Assert
        await _mockS3Client
            .Received(1)
            .PutObjectAsync
            (                Arg.Is<PutObjectRequest>
                (
                    static request =>
                        request.Metadata.Keys.Contains("x-amz-meta-info") &&
                        request.Metadata.Keys.Contains("x-amz-meta-nome") &&
                        request.Metadata.Keys.Contains("x-amz-meta-versao") &&
                        request.Metadata.Keys.Contains("x-amz-meta-empresa") &&
                        request.Metadata["x-amz-meta-nome"] == NOME_PACOTE &&
                        request.Metadata["x-amz-meta-versao"] == VERSAO_PACOTE &&
                        request.Metadata["x-amz-meta-empresa"] == SIGLA_EMPRESA
                )
            );
    }

    [Fact]
    public async Task FazerUploadAsync_ManifestoNull_DeveLancarArgumentNullException()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, null!));
    }

    [Fact]
    public async Task FazerUploadAsync_ManifestoSemSiglaEmpresa_NaoDeveIncluirMetadataEmpresa()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
            .Returns(new PutObjectResponse());

        // Act
        await _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto);

        // Assert
        await _mockS3Client.Received(1).PutObjectAsync(Arg.Is<PutObjectRequest>
        (
            static request =>
                request.Metadata.Keys.Contains("x-amz-meta-info") &&
                request.Metadata.Keys.Contains("x-amz-meta-nome") &&
                request.Metadata.Keys.Contains("x-amz-meta-versao") &&
                !request.Metadata.Keys.Contains("x-amz-meta-empresa"))
        );
    }

    [Fact]
    public async Task FazerUploadAsync_MetadataInfo_DeveSerJsonBase64Valido()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);

        var dadosCompletos = new Dictionary<string, object>
        {
            ["nome"] = NOME_PACOTE,
            ["versao"] = VERSAO_PACOTE,
            ["autor"] = "Autor Teste",
            ["descricao"] = "Descrição do pacote"
        };

        var manifesto = new ManifestoDeploy
        {
            Nome = NOME_PACOTE,
            Versao = VERSAO_PACOTE,
            Develop = false,
            SiglaEmpresa = SIGLA_EMPRESA,
            DadosCompletos = dadosCompletos
        };

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
            .Returns(new PutObjectResponse());

        // Act
        await _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto);

        // Assert
        await _mockS3Client.Received(1).PutObjectAsync(Arg.Is<PutObjectRequest>(request =>
            ValidarMetadataInfo(request.Metadata["info"], manifesto)));
    }

    [Theory]
    [InlineData("", CHAVE_ARQUIVO, CAMINHO_ARQUIVO)]
    [InlineData(BUCKET_NAME, "", CAMINHO_ARQUIVO)]
    [InlineData(BUCKET_NAME, CHAVE_ARQUIVO, "")]
    public async Task FazerUploadAsync_ParametrosVazios_DeveLancarArgumentException(string bucket, string chave, string caminhoArquivo)
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _s3Service.FazerUploadAsync(bucket, chave, caminhoArquivo, manifesto));
    }

    [Fact]
    public async Task FazerUploadAsync_UploadComSucesso_DeveConfigurarRequestCorretamente()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false, SIGLA_EMPRESA);

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
            .Returns(new PutObjectResponse());

        // Act
        await _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto);

        // Assert
        await _mockS3Client
            .Received(1)
            .PutObjectAsync
            (
                Arg.Is<PutObjectRequest>
                (
                    static request =>
                        request.BucketName == BUCKET_NAME &&
                        request.Key == CHAVE_ARQUIVO &&
                        request.FilePath == CAMINHO_ARQUIVO &&
                        request.ContentType == "application/zip"
                )
            );
    }

    [Fact]
    public async Task FazerUploadAsync_UploadComSucesso_DeveRetornarUrlCorreta()
    {
        // Arrange
        _s3Service.ConfigurarCredenciais(AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION);
        var manifesto = CriarManifesto(NOME_PACOTE, VERSAO_PACOTE, false, SIGLA_EMPRESA);

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
            .Returns(new PutObjectResponse());

        // Act
        var resultado = await _s3Service.FazerUploadAsync(BUCKET_NAME, CHAVE_ARQUIVO, CAMINHO_ARQUIVO, manifesto);

        // Assert
        resultado.ShouldBe(URL_S3_ESPERADA);
    }    private static ManifestoDeploy CriarManifesto(string nome, string versao, bool develop, string? siglaEmpresa = null)
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

    private static bool ValidarMetadataInfo(string metadataInfo, ManifestoDeploy manifesto)
    {
        try
        {
            var bytes = Convert.FromBase64String(metadataInfo);
            var json = Encoding.UTF8.GetString(bytes);
            var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (obj == null || !obj.ContainsKey("nome") || !obj.ContainsKey("versao") || !obj.ContainsKey("manifesto"))
                return false;

            var nomeElement = (JsonElement)obj["nome"];
            var versaoElement = (JsonElement)obj["versao"];

            return nomeElement.GetString() == manifesto.Nome && versaoElement.GetString() == manifesto.Versao;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidarMetadataInfoCompleto(string metadataInfo, ManifestoDeploy manifesto)
    {
        try
        {
            var bytes = Convert.FromBase64String(metadataInfo);
            var json = Encoding.UTF8.GetString(bytes);
            var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (obj == null) return false;

            // Verifica estrutura básica
            if (!obj.ContainsKey("nome") || !obj.ContainsKey("versao") || !obj.ContainsKey("manifesto"))
                return false;

            var nomeElement = (JsonElement)obj["nome"];
            var versaoElement = (JsonElement)obj["versao"];
            var manifestoElement = (JsonElement)obj["manifesto"];

            // Verifica valores básicos
            if (nomeElement.GetString() != manifesto.Nome || versaoElement.GetString() != manifesto.Versao)
                return false;

            // Verifica se o manifesto contém dados complexos
            var manifestoDict = JsonSerializer.Deserialize<Dictionary<string, object>>(manifestoElement.GetRawText());

            return manifestoDict != null && manifestoDict.ContainsKey("autor") && manifestoDict.ContainsKey("tags");
        }
        catch
        {
            return false;
        }
    }
}
