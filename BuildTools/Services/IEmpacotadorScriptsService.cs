namespace BuildTools.Services;

/// <summary>
/// Serviço para empacotamento de scripts SQL conforme regras do empacotar_scripts.py.
/// </summary>
public interface IEmpacotadorScriptsService
{
    /// <summary>
    /// Lista todos os arquivos de scripts recursivamente (incluindo subpastas) para empacotar no zip.
    /// </summary>
    /// <param name="pasta">Pasta de origem.</param>
    /// <returns>Tuplas (caminho completo, caminho relativo para o zip).</returns>
    IEnumerable<(string CaminhoCompleto, string CaminhoNoZip)> ListarArquivosComRelativo(string pasta);

    /// <summary>
    /// Lista subpastas válidas para empacotamento (padrão regex: dois dígitos no início).
    /// </summary>
    /// <param name="pasta">Pasta base.</param>
    /// <returns>Lista de subpastas válidas.</returns>
    IEnumerable<string> ListarSubpastasValidas(string pasta);

    /// <summary>
    /// Verifica se existe um config.json válido na pasta.
    /// </summary>
    /// <param name="pasta">Caminho da pasta.</param>
    /// <returns>True se existir e for válido, senão false.</returns>
    /// <exception cref="InvalidOperationException">
    /// Ocorre se o arquivo config.json não for válido.
    /// </exception>
    bool TemConfigJson(string pasta);
}