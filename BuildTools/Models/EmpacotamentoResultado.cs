namespace BuildTools.Models;

/// <summary>
/// Resultado do empacotamento, incluindo caminho do pacote e arquivos incluídos.
/// </summary>
/// <param name="CaminhoPacote">
/// O caminho completo do pacote gerado.
/// </param>
/// <param name="ArquivosIncluidos">
/// A lista de arquivos incluídos no pacote, com caminhos relativos.
/// </param>
public sealed record EmpacotamentoResultado(string CaminhoPacote, List<string> ArquivosIncluidos);
