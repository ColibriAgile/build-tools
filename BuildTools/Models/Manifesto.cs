using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildTools.Models;

/// <summary>
/// Representa o manifesto de empacotamento de arquivos.
/// </summary>
public sealed class Manifesto
{
    /// <summary>
    /// Nome do pacote.
    /// </summary>
    [JsonPropertyName("nome")]
    public required string Nome { get; set; }

    /// <summary>
    /// Versão do pacote.
    /// </summary>
    [JsonPropertyName("versao")]
    public required string Versao { get; set; }

    /// <summary>
    /// Lista de arquivos do pacote.
    /// </summary>
    [JsonPropertyName("arquivos")]
    public List<ManifestoArquivo> Arquivos { get; set; } = [];

    /// <summary>
    /// Outras propriedades dinâmicas do manifesto.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extras { get; set; }
}

/// <summary>
/// Representa um arquivo listado no manifesto.
/// </summary>
public sealed class ManifestoArquivo
{
    /// <summary>
    /// Nome do arquivo.
    /// </summary>
    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    /// <summary>
    /// Expressão regular para pattern de nome (usado apenas na leitura do manifesto).
    /// </summary>
    [JsonPropertyName("_pattern_nome")]
    public string? PatternNome { get; set; }

    /// <summary>
    /// Destino do arquivo no pacote.
    /// </summary>
    [JsonPropertyName("destino")]
    public string? Destino { get; set; }

    /// <summary>
    /// Outras propriedades dinâmicas do arquivo.
    /// </summary>
    [JsonExtensionData]
    [ExcludeFromCodeCoverage]
    public Dictionary<string, object>? Extras { get; set; }
}

