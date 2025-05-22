using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Services;

/// <inheritdoc/>
public sealed partial class EmpacotadorScriptsService : IEmpacotadorScriptsService
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;
    private readonly IZipService _zipService;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="EmpacotadorScriptsService"/>.
    /// </summary>
    /// <param name="fileSystem">Abstração do sistema de arquivos.</param>
    /// <param name="console">
    /// AnsiConsole para exibir mensagens no console.
    /// </param>
    /// <param name="zipService">
    /// Abstração do serviço de compactação de arquivos zip.
    /// </param>
    public EmpacotadorScriptsService(IFileSystem fileSystem, IAnsiConsole console, IZipService zipService)
    {
        _fileSystem = fileSystem;
        _console = console;
        _zipService = zipService;
    }

    /// <inheritdoc/>
    public bool TemConfigJson(string pasta)
    {
        var arq = Path.Combine(pasta, "config.json");

        if (!_fileSystem.File.Exists(arq))
            return false;

        try
        {
            var jsonText = _fileSystem.File.ReadAllText(arq);
            using var doc = JsonDocument.Parse(jsonText);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Arquivo config.json inválido em {pasta}: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public EmpacotamentoScriptResultado Empacotar(string pasta, string saida, bool padronizarNomes, bool silencioso)
    {
        try
        {
            if (!_fileSystem.Directory.Exists(pasta))
            {
                _console.MarkupLineInterpolated($"[red][[ERROR]] A pasta de origem não existe: {pasta}[/]");

                throw new DirectoryNotFoundException($"A pasta de origem não existe: {pasta}");
            }

            _fileSystem.Directory.CreateDirectory(saida);

            if (!silencioso)
                _console.MarkupLineInterpolated($"[yellow][[INFO]] Pasta de saída criada: {saida}[/]");

            var arquivosGerados = new List<string>();
            var arquivosRenomeados = new List<(string Antigo, string Novo)>();

            var sw = Stopwatch.StartNew();

            if (!silencioso)
                _console.MarkupLine("[blue][[INFO]] Iniciando empacotamento...[/]");

            ProcessarEmpacotamento(pasta, saida, silencioso, arquivosGerados);

            if (padronizarNomes)
                arquivosRenomeados = PadronizarNomesArquivos(arquivosGerados, silencioso);

            sw.Stop();

            if (!silencioso)
                _console.MarkupLineInterpolated($"[green][[SUCCESS]] Todos os pacotes gerados com sucesso em {sw.Elapsed.TotalSeconds:N1}s.[/]");

            return new EmpacotamentoScriptResultado(arquivosGerados, arquivosRenomeados);
        }
        catch (Exception ex)
        {
            _console.WriteException(ex, ExceptionFormats.ShortenEverything);

            throw;
        }
    }

    private void ProcessarEmpacotamento(string pasta, string saida, bool silencioso, List<string> arquivosGerados)
    {
        if (TemConfigJson(pasta))
        {
            var destinoZip = Path.Combine(saida, "_scripts.zip");
            EmpacotarScriptsDireto(pasta, destinoZip, silencioso);
            arquivosGerados.Add(destinoZip);

            return;
        }

        foreach (var subpasta in ListarSubpastasValidas(pasta))
        {
            var nome = Path.GetFileName(subpasta);
            var destinoZip = Path.Combine(saida, $"_scripts{nome}.zip");
            EmpacotarScriptsDireto(subpasta, destinoZip, silencioso);
            arquivosGerados.Add(destinoZip);
        }
    }

    private List<(string Antigo, string Novo)> PadronizarNomesArquivos(IEnumerable<string> arquivos, bool silencioso)
    {
        var regex = RegexPadronizaNomes();
        var renomeados = new List<(string, string)>();

        foreach (var (arquivo, nome, pasta, match) in arquivos
            .Select(arq => (arq, Path.GetFileName(arq), Path.GetDirectoryName(arq), regex.Match(Path.GetFileName(arq))))
            .Where(static res => res.Item4.Success))
        {
            var parte2 = match.Groups[2].Value;
            var parte3 = match.Groups[3].Value;
            var novoNome = $"scripts{parte2}{parte3}.zip";
            var novoCaminho = Path.Combine(pasta!, novoNome);

            try
            {
                _fileSystem.File.Move(arquivo, novoCaminho, overwrite: true);
            }
            catch (Exception ex)
            {
                _console.MarkupLineInterpolated($"[red][[ERROR]] Erro ao renomear arquivo {arquivo} para {novoCaminho}: {ex.Message}[/]");

                throw;
            }

            renomeados.Add((arquivo, novoCaminho));

            if (!silencioso)
                _console.MarkupLineInterpolated($"[blue][[INFO]] Arquivo renomeado: {nome} » {novoNome}[/]");
        }

        return renomeados;
    }

    private void EmpacotarScriptsDireto(string pastaOrigem, string destinoZip, bool silencioso)
    {
        var arquivos = ListarArquivosComRelativo(pastaOrigem).ToList();

        if (arquivos.Count <= 1) // Só tem o config.json
        {
            if (!silencioso)
                _console.MarkupLineInterpolated($"[yellow][[WARN]] Nenhum arquivo de script encontrado em: {pastaOrigem}[/]");

            return;
        }

        if (_fileSystem.File.Exists(destinoZip))
            _fileSystem.File.Delete(destinoZip);

        _zipService.CompactarZip(pastaOrigem, arquivos, destinoZip);

        if (!silencioso)
            _console.MarkupLineInterpolated($"[green][[SUCCESS]] Pacote gerado: {destinoZip}[/]");
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

    [System.Text.RegularExpressions.GeneratedRegex(@"^_scripts(\d{0,2})(\S+)?\.zip$")]
    private static partial System.Text.RegularExpressions.Regex RegexPadronizaNomes();
}
