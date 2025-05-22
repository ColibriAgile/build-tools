namespace BuildTools.Models;

/// <summary>
/// Resultado do empacotamento, incluindo caminho do pacote e arquivos incluídos.
/// </summary>
public sealed class EmpacotamentoResultado
{
    /// <summary>
    /// Caminho do pacote gerado.
    /// </summary>
    public required string CaminhoPacote { get; init; }

    /// <summary>
    /// Lista dos arquivos incluídos no pacote.
    /// </summary>
    public required List<string> ArquivosIncluidos { get; init; }
}
