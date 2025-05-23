namespace BuildTools.Models;

/// <summary>
/// Representa o resultado do empacotamento de scripts SQL.
/// </summary>
/// <param name="ArquivosGerados">
/// A lista de arquivos gerados durante o empacotamento.
/// </param>
/// <param name="ArquivosRenomeados">
/// A lista de arquivos renomeados, onde cada tupla contém o nome antigo e o novo.
/// </param>
public record EmpacotamentoScriptResultado(List<string> ArquivosGerados, List<(string Antigo, string Novo)> ArquivosRenomeados);
