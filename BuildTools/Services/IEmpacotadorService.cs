namespace BuildTools.Services;

/// <summary>
/// Serviço responsável por empacotar arquivos conforme manifesto.
/// </summary>
public interface IEmpacotadorService
{
    /// <summary>
    /// Empacota arquivos de uma pasta conforme manifesto.
    /// </summary>
    /// <param name="pasta">Pasta de origem dos arquivos.</param>
    /// <param name="pastaSaida">Pasta de saída do pacote.</param>
    /// <param name="senha">Senha do pacote zip (opcional).</param>
    /// <param name="versao">Versão do pacote (opcional).</param>
    /// <param name="develop">Indica se o pacote é de desenvolvimento.</param>
    /// <returns>Caminho do pacote gerado.</returns>
    string Empacotar(string pasta, string pastaSaida, string senha = "", string? versao = null, bool develop = false);
}