namespace BuildTools.Services;

using BuildTools.Models;

/// <summary>
/// Serviço responsável por uploads para AWS S3.
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Configura as credenciais AWS.
    /// </summary>
    /// <param name="accessKey">AWS Access Key.</param>
    /// <param name="secretKey">AWS Secret Key.</param>
    /// <param name="region">AWS Region.</param>
    void ConfigurarCredenciais(string accessKey, string secretKey, string region);

    /// <summary>
    /// Verifica se um arquivo já existe no S3.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="chave">Chave do objeto no S3.</param>
    /// <returns>True se o arquivo existir, false caso contrário.</returns>
    Task<bool> ArquivoExisteAsync(string bucket, string chave);

    /// <summary>
    /// Faz upload de um arquivo para o S3.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="chave">Chave do objeto no S3.</param>
    /// <param name="caminhoArquivo">Caminho local do arquivo.</param>
    /// <param name="manifesto">Dados do manifesto para metadados.</param>
    /// <returns>URL do arquivo no S3.</returns>
    Task<string> FazerUploadAsync(string bucket, string chave, string caminhoArquivo, ManifestoDeploy manifesto);
}
