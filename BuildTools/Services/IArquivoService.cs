namespace BuildTools.Services;

using BuildTools.Constants;

/// <summary>
/// Serviço utilitário para manipulação de arquivos e diretórios.
/// </summary>
public interface IArquivoService
{
    /// <summary>
    /// Exclui arquivos em uma pasta que começam com o prefixo informado e possuem a extensão informada.
    /// </summary>
    /// <param name="pasta">Pasta de busca.</param>
    /// <param name="prefixo">Prefixo do nome do arquivo.</param>
    /// <param name="extensao">Extensão do arquivo.</param>
    void ExcluirComPrefixo(string pasta, string prefixo, string extensao = EmpacotadorConstantes.EXTENSAO_PACOTE);
}
