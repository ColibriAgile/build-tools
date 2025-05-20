// Arquivo: d:\projetos\BuildTools\BuildTools\Services\IArquivoService.cs

namespace BuildTools.Services;

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
    void ExcluirComPrefixo(string pasta, string prefixo, string extensao);

    /// <summary>
    /// Copia um arquivo para a pasta de QA, se configurado.
    /// </summary>
    /// <param name="nomeArquivo">Nome do arquivo a ser copiado.</param>
    /// <param name="prefixo">Prefixo do arquivo.</param>
    /// <param name="origem">Caminho de origem.</param>
    void CopiarParaQa(string nomeArquivo, string prefixo, string origem);
}
