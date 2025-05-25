using System.IO.Abstractions;
using Spectre.Console;
using BuildTools.Constants;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço utilitário para manipulação de arquivos e diretórios.
/// </summary>
/// <inheritdoc cref="IArquivoService"/>
public sealed class ArquivoService : IArquivoService
{
    private readonly IFileSystem _fileSystem;

    public ArquivoService(IFileSystem fileSystem, IAnsiConsole console)
        => _fileSystem = fileSystem;

    public void ExcluirComPrefixo
    (
        string pasta,
        string prefixo,
        string extensao = EmpacotadorConstantes.EXTENSAO_PACOTE
    )
    {
        var arquivos = _fileSystem.Directory.GetFiles(pasta, $"{prefixo}*{extensao}");

        foreach (var arquivo in arquivos)
            _fileSystem.File.Delete(arquivo);
    }
}
