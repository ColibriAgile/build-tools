namespace BuildTools.Services;

/// <summary>
/// Interface para serviço de compactação ZIP.
/// </summary>
public interface IZipService
{
    /// <summary>
    /// Compacta arquivos em um ZIP.
    /// </summary>
    /// <param name="pastaOrigem">Pasta de origem dos arquivos</param>
    /// <param name="arquivos">Lista de arquivos (nomes) a compactar</param>
    /// <param name="caminhoZip">Caminho do arquivo ZIP de saída</param>
    /// <param name="senha">Senha do ZIP (opcional, pode ser ignorada)</param>
    void CompactarZip
    (
        string pastaOrigem,
        List<string> arquivos,
        string caminhoZip,
        string senha
    );
}
