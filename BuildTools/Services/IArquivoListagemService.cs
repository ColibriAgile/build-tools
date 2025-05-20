using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Serviço para geração e ordenação da lista de arquivos a serem empacotados.
/// </summary>
public interface IArquivoListagemService
{
    /// <summary>
    /// Obtém a lista de arquivos a serem empacotados, atualizando o manifesto.
    /// </summary>
    /// <param name="pasta">Pasta de origem.</param>
    /// <param name="manifesto">Manifesto a ser atualizado.</param>
    /// <returns>Lista de nomes dos arquivos a empacotar.</returns>
    List<string> ObterArquivos(string pasta, Manifesto manifesto);
}
