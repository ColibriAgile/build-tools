namespace BuildTools.Models;

/// <summary>
/// Representa o resultado do processo de deploy.
/// </summary>
public sealed class DeployResultado
{
    /// <summary>
    /// Lista de arquivos que foram enviados com sucesso.
    /// </summary>
    public List<DeployArquivo> ArquivosEnviados { get; set; } = [];

    /// <summary>
    /// Lista de arquivos que falharam no envio.
    /// </summary>
    public List<DeployArquivo> ArquivosFalharam { get; set; } = [];

    /// <summary>
    /// Lista de arquivos que foram ignorados (já existiam e não foi usado --forcar).
    /// </summary>
    public List<DeployArquivo> ArquivosIgnorados { get; set; } = [];

    /// <summary>
    /// Ambiente de deploy utilizado.
    /// </summary>
    public string Ambiente { get; set; } = string.Empty;

    /// <summary>
    /// URL do marketplace utilizada.
    /// </summary>
    public string UrlMarketplace { get; set; } = string.Empty;

    /// <summary>
    /// Indica se foi uma execução simulada.
    /// </summary>
    public bool Simulado { get; set; }

    /// <summary>
    /// Tempo total de execução.
    /// </summary>
    public TimeSpan TempoExecucao { get; set; }
}

/// <summary>
/// Representa um arquivo processado durante o deploy.
/// </summary>
public sealed class DeployArquivo
{
    /// <summary>
    /// Caminho do arquivo .cmpkg.
    /// </summary>
    public string CaminhoArquivo { get; set; } = string.Empty;

    /// <summary>
    /// Nome do arquivo no S3.
    /// </summary>
    public string NomeArquivoS3 { get; set; } = string.Empty;

    /// <summary>
    /// Caminho do manifesto correspondente.
    /// </summary>
    public string CaminhoManifesto { get; set; } = string.Empty;

    /// <summary>
    /// Dados do manifesto.
    /// </summary>
    public ManifestoDeploy? Manifesto { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver).
    /// </summary>
    public string? MensagemErro { get; set; }

    /// <summary>
    /// URL final do arquivo no S3.
    /// </summary>
    public string? UrlS3 { get; set; }
}

/// <summary>
/// Representa os dados relevantes do manifesto para deploy.
/// </summary>
public sealed class ManifestoDeploy
{
    /// <summary>
    /// Nome do pacote.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Versão do pacote.
    /// </summary>
    public string Versao { get; set; } = string.Empty;

    /// <summary>
    /// Indica se é versão de desenvolvimento.
    /// </summary>
    public bool Develop { get; set; }

    /// <summary>
    /// Sigla da empresa (opcional).
    /// </summary>
    public string? SiglaEmpresa { get; set; }

    /// <summary>
    /// Dados completos do manifesto em formato dicionário.
    /// </summary>
    public Dictionary<string, object> DadosCompletos { get; set; } = [];
}
