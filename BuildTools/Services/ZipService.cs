using System.IO.Compression;
using System.IO.Abstractions;

namespace BuildTools.Services;

/// <summary>
/// Implementação padrão de IZipService usando System.IO.Compression.
/// </summary>
/// <param name="fileSystem">
/// Abstração do sistema de arquivos.
/// </param>
/// <inheritdoc cref="IZipService"/>
public sealed class ZipService(IFileSystem fileSystem) : IZipService
{

    /// <inheritdoc />
    public void CompactarZip
    (
        string pastaOrigem,
        List<string> arquivos,
        string caminhoZip,
        string? senha
    )
    {
        using var zip = ZipFile.Open
        (
            caminhoZip,
            ZipArchiveMode.Create
        );

        foreach (var (caminhoArquivo, nomeArquivo) in arquivos           
            .Select(arq => (Path.Combine(pastaOrigem, arq), arq))
            .Where(x => fileSystem.File.Exists(x.Item1)))
        {
            using var stream = fileSystem.File.OpenRead(caminhoArquivo);
            var entry = zip.CreateEntry(nomeArquivo); // Garante que só o nome do arquivo vai para o zip
            using var entryStream = entry.Open();
            stream.CopyTo(entryStream);
        }
    }

    /// <inheritdoc />
    public void CompactarZip
    (
        string pastaOrigem,
        List<(string caminhoCompleto, string caminhoZip)> arquivos,
        string caminhoZip,
        string? senha = null
    )
    {
        using var zip = ZipFile.Open(caminhoZip, ZipArchiveMode.Create);

        foreach (var (caminhoCompleto, caminhoRelativo) in arquivos
            .Where(x => fileSystem.File.Exists(x.caminhoCompleto)))
        {
            zip.CreateEntryFromFile(caminhoCompleto, caminhoRelativo);
        }
    }
}
