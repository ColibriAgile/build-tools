using System.IO.Abstractions;
using System.Text.Json;
using BuildTools.Models;
using BuildTools.Validation;
using Spectre.Console;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço de deploy.
/// </summary>
/// <remarks>
/// Inicializa uma nova instância da classe <see cref="DeployService"/>.
/// </remarks>
/// <param name="fileSystem">Sistema de arquivos para operações de I/O.</param>
/// <param name="s3Service">Serviço AWS S3 para uploads.</param>
/// <param name="marketplaceService">Serviço de notificação do marketplace.</param>
/// <param name="console">Console para saída de informações.</param>
public sealed class DeployService
(
    IFileSystem fileSystem,
    IS3Service s3Service,
    IMarketplaceService marketplaceService,
    IAnsiConsole console
) : IDeployService
{
    private const string BUCKET_NAME = "ncr-colibri";
    private const string PREFIX_PRODUCAO = "packages";
    private const string PREFIX_DESENVOLVIMENTO = "packages-dev";
    private const string PREFIX_STAGE = "packages-stage";

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
    public async Task<DeployResultado> ExecutarDeployAsync
    (
        string pasta,
        string ambiente,
        string? urlMarketplace = null,
        bool simulado = false,
        bool forcar = false,
        string? awsAccessKey = null,
        string? awsSecretKey = null,
        string? awsRegion = null
    )
    {
        var inicioExecucao = DateTime.UtcNow;
        AmbienteValidator.ValidarAmbiente(ambiente);

        var credenciais = ObterCredenciaisAws(awsAccessKey, awsSecretKey, awsRegion);
        var urlMarketplaceFinal = marketplaceService.ObterUrlMarketplace(ambiente, urlMarketplace);

        if (!simulado)
        {
            s3Service.ConfigurarCredenciais(credenciais.AccessKey, credenciais.SecretKey, credenciais.Region);
        }

        var resultado = new DeployResultado
        {
            Ambiente = ambiente,
            UrlMarketplace = urlMarketplaceFinal,
            Simulado = simulado
        };

        var arquivos = await EncontrarArquivosParaDeployAsync(pasta).ConfigureAwait(false);

        foreach (var arquivo in arquivos)
        {
            await ProcessarArquivoAsync(arquivo, ambiente, urlMarketplaceFinal, simulado, forcar, resultado)
                .ConfigureAwait(false);
        }

        resultado.TempoExecucao = DateTime.UtcNow - inicioExecucao;

        return resultado;
    }

    /// <summary>
    /// Obtém as credenciais AWS priorizando parâmetros sobre variáveis de ambiente.
    /// </summary>
    /// <param name="awsAccessKey">AWS Access Key fornecida como parâmetro.</param>
    /// <param name="awsSecretKey">AWS Secret Key fornecida como parâmetro.</param>
    /// <param name="awsRegion">AWS Region fornecida como parâmetro.</param>
    /// <returns>Credenciais AWS configuradas.</returns>
    private static (string AccessKey, string SecretKey, string Region) ObterCredenciaisAws
    (
        string? awsAccessKey,
        string? awsSecretKey,
        string? awsRegion
    )
    {
        var accessKey = awsAccessKey ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")
            ?? throw new InvalidOperationException("AWS Access Key não informada. Configure via parâmetro --aws-access-key ou variável AWS_ACCESS_KEY_ID");

        var secretKey = awsSecretKey ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")
            ?? throw new InvalidOperationException("AWS Secret Key não informada. Configure via parâmetro --aws-secret-key ou variável AWS_SECRET_ACCESS_KEY");

        var region = awsRegion ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1"; return (accessKey, secretKey, region);
    }

    /// <summary>
    /// Encontra todos os arquivos .dat e .cmpkg na pasta para deploy.
    /// </summary>
    /// <param name="pasta">Pasta para buscar os arquivos.</param>
    /// <returns>Lista de arquivos encontrados para deploy.</returns>
    private async Task<List<DeployArquivo>> EncontrarArquivosParaDeployAsync(string pasta)
    {
        var arquivos = new List<DeployArquivo>();

        if (!fileSystem.Directory.Exists(pasta))
            throw new DirectoryNotFoundException($"Pasta não encontrada: {pasta}");

        var arquivosManifesto = fileSystem.Directory.GetFiles(pasta, "*.dat");

        foreach (var manifestoPath in arquivosManifesto)
        {
            try
            {
                var manifesto = await LerManifestoAsync(manifestoPath).ConfigureAwait(false);
                var nomeArquivoCmpkg = CriarNomeArquivoCmpkg(manifesto);
                var caminhoArquivoCmpkg = fileSystem.Path.Combine(pasta, nomeArquivoCmpkg);
                console.MarkupLineInterpolated($"[blue][[INFO]] Procurando arquivo: {nomeArquivoCmpkg}[/]");

                if (!fileSystem.File.Exists(caminhoArquivoCmpkg))
                {
                    console.MarkupLineInterpolated($"[yellow][[WARN]] Arquivo .cmpkg não encontrado: {nomeArquivoCmpkg}[/]");

                    // Procurar por qualquer arquivo .cmpkg na pasta
                    var arquivosCmpkg = fileSystem.Directory.GetFiles(pasta, "*.cmpkg");

                    if (arquivosCmpkg.Length == 0)
                    {
                        console.MarkupLineInterpolated($"[yellow][[WARN]] Arquivo .cmpkg não encontrado para manifesto {fileSystem.Path.GetFileName(manifestoPath)}[/]");

                        continue;
                    }

                    console.MarkupLineInterpolated($"[blue][[INFO]] Renomeando arquivo {fileSystem.Path.GetFileName(arquivosCmpkg[0])} para {nomeArquivoCmpkg}[/]");
                    fileSystem.File.Move(arquivosCmpkg[0], caminhoArquivoCmpkg, true);
                }

                var nomeArquivoS3 = fileSystem.Path.GetFileName(caminhoArquivoCmpkg);

                var arquivo = new DeployArquivo
                {
                    CaminhoArquivo = caminhoArquivoCmpkg,
                    CaminhoManifesto = manifestoPath,
                    NomeArquivoS3 = nomeArquivoS3,
                    Manifesto = manifesto
                };

                arquivos.Add(arquivo);
            }
            catch (Exception ex)
            {
                console.MarkupLineInterpolated($"[red][[ERROR]] Erro ao processar manifesto {fileSystem.Path.GetFileName(manifestoPath)}: {ex.Message}[/]");
            }
        }

        return arquivos;
    }

    /// <summary>
    /// Lê e deserializa um arquivo de manifesto.
    /// </summary>
    /// <param name="caminhoManifesto">Caminho para o arquivo de manifesto.</param>
    /// <returns>Dados do manifesto.</returns>
    private async Task<ManifestoDeploy> LerManifestoAsync(string caminhoManifesto)
    {
        var json = await fileSystem.File.ReadAllTextAsync(caminhoManifesto).ConfigureAwait(false);

        var dadosCompletos = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
            ?? throw new InvalidOperationException($"Não foi possível deserializar o manifesto: {caminhoManifesto}");

        var manifesto = new ManifestoDeploy
        {
            DadosCompletos = dadosCompletos
        };

        if (dadosCompletos.TryGetValue("nome", out var nomeObj) && nomeObj is JsonElement nomeElement)
            manifesto.Nome = nomeElement.GetString() ?? string.Empty;

        if (dadosCompletos.TryGetValue("versao", out var versaoObj) && versaoObj is JsonElement versaoElement)
            manifesto.Versao = versaoElement.GetString() ?? string.Empty;

        if (dadosCompletos.TryGetValue("develop", out var developObj) && developObj is JsonElement developElement)
            manifesto.Develop = developElement.GetBoolean();

        if (dadosCompletos.TryGetValue("siglaEmpresa", out var empresaObj) && empresaObj is JsonElement empresaElement)
            manifesto.SiglaEmpresa = empresaElement.GetString();

        return manifesto;
    }

    /// <summary>
    /// Cria o nome do arquivo .cmpkg baseado no manifesto.
    /// </summary>
    /// <param name="manifesto">Dados do manifesto.</param>
    /// <returns>Nome do arquivo .cmpkg.</returns>
    private static string CriarNomeArquivoCmpkg(ManifestoDeploy manifesto)
    {
        var versaoLimpa = manifesto.Versao.Replace(".", "_");
        var nomeLimpo = manifesto.Nome.ToLowerInvariant();

        return !string.IsNullOrEmpty(manifesto.SiglaEmpresa)
            ? $"{manifesto.SiglaEmpresa.ToLowerInvariant()}-{nomeLimpo}_{versaoLimpa}.cmpkg"
            : $"{nomeLimpo}_{versaoLimpa}.cmpkg";
    }

    /// <summary>
    /// Processa um arquivo individual para deploy.
    /// </summary>
    /// <param name="arquivo">Arquivo a ser processado.</param>
    /// <param name="ambiente">Ambiente de deploy.</param>
    /// <param name="urlMarketplace">URL do marketplace.</param>
    /// <param name="simulado">Indica se é uma execução simulada.</param>
    /// <param name="forcar">Força o upload mesmo se o arquivo já existir.</param>
    /// <param name="resultado">Resultado do deploy para atualizar.</param>
    private async Task ProcessarArquivoAsync
    (
        DeployArquivo arquivo,
        string ambiente,
        string urlMarketplace,
        bool simulado,
        bool forcar,
        DeployResultado resultado)
    {
        try
        {
            var prefixo = ObterPrefixoS3(ambiente, arquivo.Manifesto!.Develop);
            var chaveS3 = $"{prefixo}/{arquivo.NomeArquivoS3}";

            if (!simulado && !forcar)
            {
                var arquivoExiste = await s3Service.ArquivoExisteAsync(BUCKET_NAME, chaveS3).ConfigureAwait(false);

                if (arquivoExiste)
                {
                    arquivo.MensagemErro = "Arquivo já existe no S3";
                    resultado.ArquivosIgnorados.Add(arquivo);

                    return;
                }
            }

            if (!simulado)
            {
                arquivo.UrlS3 = await s3Service.FazerUploadAsync(BUCKET_NAME, chaveS3, arquivo.CaminhoArquivo, arquivo.Manifesto!)
                    .ConfigureAwait(false);

                var notificacaoSucesso = await marketplaceService.NotificarPacoteAsync(urlMarketplace, arquivo.Manifesto!)
                    .ConfigureAwait(false);

                if (!notificacaoSucesso)
                    console.MarkupLineInterpolated($"[yellow][[WARN]] Falha ao notificar o marketplace para {arquivo.NomeArquivoS3}[/]");
            }
            else
            {
                arquivo.UrlS3 = $"https://{BUCKET_NAME}.s3.amazonaws.com/{chaveS3}";
            }

            resultado.ArquivosEnviados.Add(arquivo);
        }
        catch (Exception ex)
        {
            arquivo.MensagemErro = ex.Message;
            resultado.ArquivosFalharam.Add(arquivo);
        }
    }

    /// <summary>
    /// Obtém o prefixo S3 baseado no ambiente e flag de desenvolvimento.
    /// </summary>
    /// <param name="ambiente">Ambiente de deploy.</param>
    /// <param name="develop">Indica se é versão de desenvolvimento.</param>
    /// <returns>Prefixo para usar no S3.</returns>
    private static string ObterPrefixoS3(string ambiente, bool develop)
    {
        if (develop)
            return PREFIX_DESENVOLVIMENTO;

        var isStage = string.Equals(Environment.GetEnvironmentVariable("STAGE"), "true", StringComparison.OrdinalIgnoreCase);

        if (isStage || string.Equals(ambiente, "stage", StringComparison.OrdinalIgnoreCase))
            return PREFIX_STAGE;

        return ambiente.ToLowerInvariant() switch
        {
            "producao" => PREFIX_PRODUCAO,
            var _ => PREFIX_DESENVOLVIMENTO
        };
    }
}
