using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Serviço responsável por orquestrar o processo de deploy.
/// </summary>
public interface IDeployService
{
    /// <summary>
    /// Executa o deploy de pacotes para AWS S3 e notifica o marketplace.
    /// </summary>
    /// <param name="pasta">Pasta contendo os arquivos .dat e .cmpkg.</param>
    /// <param name="ambiente">Ambiente de deploy (desenvolvimento, producao, stage).</param>
    /// <param name="urlMarketplace">URL do marketplace (opcional).</param>
    /// <param name="simulado">Indica se é uma execução simulada.</param>
    /// <param name="forcar">Força o upload mesmo se o arquivo já existir.</param>
    /// <param name="awsAccessKey">AWS Access Key (opcional, usa variável de ambiente se não informado).</param>
    /// <param name="awsSecretKey">AWS Secret Key (opcional, usa variável de ambiente se não informado).</param>
    /// <param name="awsRegion">AWS Region (opcional, usa variável de ambiente se não informado).</param>
    /// <returns>Resultado do deploy com detalhes dos arquivos processados.</returns>
    Task<DeployResultado> ExecutarDeployAsync
    (
        string pasta,
        string ambiente,
        string? urlMarketplace = null,
        bool simulado = false,
        bool forcar = false,
        string? awsAccessKey = null,
        string? awsSecretKey = null,
        string? awsRegion = null
    );
}
