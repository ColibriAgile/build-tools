using System.IO.Abstractions;
using BuildTools.Constants;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço utilitário para manipulação de arquivos e diretórios.
/// </summary>
/// <inheritdoc cref="IArquivoService"/>
public sealed class ArquivoService(IFileSystem fileSystem) : IArquivoService
{
    public void ExcluirComPrefixo
    (
        string pasta,
        string prefixo,
        string extensao = EmpacotadorConstantes.EXTENSAO_PACOTE
    )
    {
        var arquivos = fileSystem.Directory.GetFiles(pasta, $"{prefixo}*{extensao}");

        foreach (var arquivo in arquivos)
            fileSystem.File.Delete(arquivo);
    }
}
