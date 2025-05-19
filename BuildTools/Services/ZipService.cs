using System.IO.Compression;
using System.IO.Abstractions;

namespace BuildTools.Services;

/// <summary>
/// Implementação padrão de IZipService usando System.IO.Compression.
/// </summary>
public sealed class ZipService(IFileSystem fileSystem) : IZipService
{
    private readonly IFileSystem _fileSystem = fileSystem;

    /// <inheritdoc />
    public void CompactarZip
    (
        string pastaOrigem,
        List<string> arquivos,
        string caminhoZip,
        string senha
    )
    {
        using var zip = ZipFile.Open
        (
            caminhoZip,
            ZipArchiveMode.Create
        );

        foreach (var nomeArquivo in arquivos)
        {
            var caminhoArquivo = _fileSystem.Path.Combine(pastaOrigem, nomeArquivo);

            if (!_fileSystem.File.Exists(caminhoArquivo))
            {
                continue;
            }

            using var stream = _fileSystem.File.OpenRead(caminhoArquivo);
            var entry = zip.CreateEntry(nomeArquivo);
            using var entryStream = entry.Open();
            stream.CopyTo(entryStream);
        }
    }
}
