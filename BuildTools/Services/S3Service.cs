using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço AWS S3.
/// </summary>
public sealed class S3Service : IS3Service, IDisposable
{
    private AmazonS3Client? _s3Client;

    /// <summary>
    /// Configura as credenciais AWS.
    /// </summary>
    /// <param name="accessKey">AWS Access Key.</param>
    /// <param name="secretKey">AWS Secret Key.</param>
    /// <param name="region">AWS Region.</param>
    public void ConfigurarCredenciais(string accessKey, string secretKey, string region)
    {
        _s3Client?.Dispose();

        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    /// <summary>
    /// Verifica se um arquivo já existe no S3.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="chave">Chave do objeto no S3.</param>
    /// <returns>True se o arquivo existir, false caso contrário.</returns>
    public async Task<bool> ArquivoExisteAsync(string bucket, string chave)
    {
        if (_s3Client == null)
            throw new InvalidOperationException("Credenciais AWS não configuradas. Chame ConfigurarCredenciais primeiro.");

        try
        {
            await _s3Client.GetObjectMetadataAsync(bucket, chave).ConfigureAwait(false);

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <summary>
    /// Faz upload de um arquivo para o S3.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="chave">Chave do objeto no S3.</param>
    /// <param name="caminhoArquivo">Caminho local do arquivo.</param>
    /// <param name="manifesto">Dados do manifesto para metadados.</param>
    /// <returns>URL do arquivo no S3.</returns>
    public async Task<string> FazerUploadAsync(string bucket, string chave, string caminhoArquivo, ManifestoDeploy manifesto)
    {
        if (_s3Client == null)
            throw new InvalidOperationException("Credenciais AWS não configuradas. Chame ConfigurarCredenciais primeiro.");

        var metadata = CriarMetadata(manifesto);

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = chave,
            FilePath = caminhoArquivo,
            ContentType = "application/zip"
        };

        foreach (var kvp in metadata)
        {
            request.Metadata.Add(kvp.Key, kvp.Value);
        }

        await _s3Client.PutObjectAsync(request).ConfigureAwait(false);

        return $"https://{bucket}.s3.amazonaws.com/{chave}";
    }

    /// <summary>
    /// Cria os metadados do S3 baseados no manifesto.
    /// </summary>
    /// <param name="manifesto">Dados do manifesto.</param>
    /// <returns>Dicionário com metadados para o S3.</returns>
    private static Dictionary<string, string> CriarMetadata(ManifestoDeploy manifesto)
    {
        var metadata = new Dictionary<string, string>();

        // Criar objeto InfoPacote similar ao Java
        var infoPacote = new
        {
            nome = manifesto.Nome,
            versao = manifesto.Versao,
            manifesto = manifesto.DadosCompletos
        };

        // Serializar para JSON
        var json = JsonSerializer.Serialize(infoPacote);

        // Converter para Base64
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var base64Json = Convert.ToBase64String(jsonBytes);

        metadata["info"] = base64Json;
        metadata["nome"] = manifesto.Nome;
        metadata["versao"] = manifesto.Versao;

        if (!string.IsNullOrEmpty(manifesto.SiglaEmpresa))
            metadata["empresa"] = manifesto.SiglaEmpresa;

        return metadata;
    }

    /// <summary>
    /// Libera os recursos utilizados pelo cliente S3.
    /// </summary>
    public void Dispose()
    {
        _s3Client?.Dispose();
        _s3Client = null;
    }
}
