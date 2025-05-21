using System.IO.Abstractions;
using System.Text.Json;

namespace BuildTools.Services;

/// <inheritdoc/>
public sealed partial class EmpacotadorScriptsService : IEmpacotadorScriptsService
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="EmpacotadorScriptsService"/>.
    /// </summary>
    /// <param name="fileSystem">Abstração do sistema de arquivos.</param>
    public EmpacotadorScriptsService(IFileSystem fileSystem)
        => _fileSystem = fileSystem;

    /// <inheritdoc/>
    public bool TemConfigJson(string pasta)
    {
        var arq = Path.Combine(pasta, "config.json");

        if (!_fileSystem.File.Exists(arq))
            return false;

        try
        {
            using var stream = _fileSystem.File.OpenRead(arq);
            using var doc = JsonDocument.Parse(stream);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Arquivo config.json inválido em {pasta}: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> ListarSubpastasValidas(string pasta)
    {
        var regex = RegexPastasValidas();

        return _fileSystem.Directory
            .EnumerateDirectories(pasta)
            .Where(subpasta => regex.IsMatch(_fileSystem.Path.GetFileName(subpasta))
                && TemConfigJson(subpasta));
    }

    /// <inheritdoc/>
    public IEnumerable<(string CaminhoCompleto, string CaminhoNoZip)> ListarArquivosComRelativo(string pasta)
    {
        // Inclui todos os arquivos .sql e .migration recursivamente, mantendo estrutura relativa
        var arquivos = _fileSystem.Directory
            .EnumerateFiles(pasta, "*.sql", SearchOption.AllDirectories)
            .Concat(_fileSystem.Directory.EnumerateFiles(pasta, "*.migration", SearchOption.AllDirectories))
            .ToList();

        // Sempre inclui o config.json da raiz
        var config = Path.Combine(pasta, "config.json");

        if (_fileSystem.File.Exists(config))
            arquivos.Add(config);

        foreach (var arquivo in arquivos)
        {
            var relativo = Path.GetFullPath(arquivo) == Path.GetFullPath(config)
                ? "config.json"
                : Path.GetRelativePath(pasta, arquivo);

            yield return (arquivo, relativo);
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^(\d{2})(\S+)?$")]
    private static partial System.Text.RegularExpressions.Regex RegexPastasValidas();
}
