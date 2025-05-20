using System.IO.Abstractions;
using System.Text.Json;
using Spectre.Console;

namespace BuildTools.Services;

/// <summary>
/// Serviço para empacotamento de scripts SQL conforme regras do empacotar_scripts.py.
/// </summary>
public sealed partial class EmpacotadorScriptsService
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="EmpacotadorScriptsService"/>.
    /// </summary>
    /// <param name="fileSystem">Abstração do sistema de arquivos.</param>
    /// <param name="console">Console para saída formatada.</param>
    public EmpacotadorScriptsService
    (
        IFileSystem fileSystem,
        IAnsiConsole console
    )
    {
        _fileSystem = fileSystem;
        _console = console;
    }

    /// <summary>
    /// Verifica se existe um config.json válido na pasta.
    /// </summary>
    /// <param name="pasta">Caminho da pasta.</param>
    /// <returns>True se existir e for válido, senão false.</returns>
    public bool TemConfigJson(string pasta)
    {
        var arq = _fileSystem.Path.Combine(pasta, "config.json");

        if (!_fileSystem.File.Exists(arq))
        {
            return false;
        }

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

    /// <summary>
    /// Lista subpastas válidas para empacotamento (padrão regex: dois dígitos no início).
    /// </summary>
    /// <param name="pasta">Pasta base.</param>
    /// <returns>Lista de subpastas válidas.</returns>
    public IEnumerable<string> ListarSubpastasValidas(string pasta)
    {
        var regex = RegexPastasValidas();

        return _fileSystem.Directory
            .EnumerateDirectories(pasta)
            .Where(subpasta => regex.IsMatch(_fileSystem.Path.GetFileName(subpasta))
                && TemConfigJson(subpasta));
    }

    /// <summary>
    /// Lista arquivos de script a serem empacotados.
    /// </summary>
    /// <param name="pasta">Pasta de origem.</param>
    /// <returns>Lista de arquivos .sql, .migration e config.json.</returns>
    public IEnumerable<string> ListarArquivosParaEmpacotar(string pasta)
    {
        var arquivos = new List<string>();

        arquivos.AddRange(_fileSystem.Directory
            .EnumerateFiles(pasta, "*.sql", SearchOption.TopDirectoryOnly));

        arquivos.AddRange(_fileSystem.Directory
            .EnumerateFiles(pasta, "*.migration", SearchOption.TopDirectoryOnly));

        var config = _fileSystem.Path.Combine(pasta, "config.json");

        if (_fileSystem.File.Exists(config))
        {
            arquivos.Add(config);
        }

        return arquivos;
    }

    /// <summary>
    /// Lista todos os arquivos de scripts recursivamente (incluindo subpastas) para empacotar no zip.
    /// </summary>
    /// <param name="pasta">Pasta de origem.</param>
    /// <returns>Tuplas (caminho completo, caminho relativo para o zip).</returns>
    public IEnumerable<(string CaminhoCompleto, string CaminhoNoZip)> ListarArquivosComRelativo(string pasta)
    {
        // Inclui todos os arquivos .sql e .migration recursivamente, mantendo estrutura relativa
        var arquivos = _fileSystem.Directory
            .EnumerateFiles(pasta, "*.sql", SearchOption.AllDirectories)
            .Concat(_fileSystem.Directory.EnumerateFiles(pasta, "*.migration", SearchOption.AllDirectories))
            .ToList();

        // Sempre inclui o config.json da raiz
        var config = _fileSystem.Path.Combine(pasta, "config.json");
        if (_fileSystem.File.Exists(config))
        {
            arquivos.Add(config);
        }

        foreach (var arquivo in arquivos)
        {
            string relativo;
            if (_fileSystem.Path.GetFullPath(arquivo) == _fileSystem.Path.GetFullPath(config))
            {
                relativo = "config.json";
            }
            else
            {
                relativo = _fileSystem.Path.GetRelativePath(pasta, arquivo);
            }

            yield return (arquivo, relativo);
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^(\d{2})(\S+)?$")]
    private static partial System.Text.RegularExpressions.Regex RegexPastasValidas();
}
