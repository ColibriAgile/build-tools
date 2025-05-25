using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Services;

/// <inheritdoc/>
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
public sealed partial class EmpacotadorScriptsService(IFileSystem fileSystem, IAnsiConsole console, IZipService zipService) : IEmpacotadorScriptsService
{
    /// <summary>
    /// Verifica se a pasta contém um arquivo config.json válido.
    /// </summary>
    /// <param name="pasta">
    /// Pasta a ser verificada.
    /// </param>
    /// <returns>
    /// Verdadeiro se o arquivo config.json é válido; caso contrário, falso.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Lançada quando o arquivo config.json é inválido.
    /// </exception>
    internal bool TemConfigJson(string pasta)
    {
        var arq = Path.Combine(pasta, "config.json");

        if (!fileSystem.File.Exists(arq))
            return false;

        try
        {
            var jsonText = fileSystem.File.ReadAllText(arq);
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
            if (!fileSystem.Directory.Exists(pasta))
            {
                console.MarkupLineInterpolated($"[red][[ERROR]] A pasta de origem não existe: {pasta}[/]");

                throw new DirectoryNotFoundException($"A pasta de origem não existe: {pasta}");
            }

            fileSystem.Directory.CreateDirectory(saida);

            if (!silencioso)
                console.MarkupLineInterpolated($"[yellow][[INFO]] Pasta de saída criada: {saida}[/]");

            var arquivosGerados = new List<string>();
            var arquivosRenomeados = new List<(string Antigo, string Novo)>();

            var sw = Stopwatch.StartNew();

            if (!silencioso)
                console.MarkupLine("[blue][[INFO]] Iniciando empacotamento...[/]");

            ProcessarEmpacotamento(pasta, saida, silencioso, arquivosGerados);

            if (padronizarNomes)
                arquivosRenomeados = PadronizarNomesArquivos(arquivosGerados, silencioso);

            sw.Stop();

            if (!silencioso)
                console.MarkupLineInterpolated($"[green][[SUCCESS]] Todos os pacotes gerados com sucesso em {sw.Elapsed.TotalSeconds:N1}s.[/]");

            return new EmpacotamentoScriptResultado(arquivosGerados, arquivosRenomeados);
        }
        catch (Exception ex)
        {
            console.WriteException(ex, ExceptionFormats.ShortenEverything);

            throw;
        }
    }

    /// <summary>
    /// Processa o empacotamento dos scripts.
    /// Cria pacotes zip para cada subpasta válida dentro da pasta especificada.
    /// </summary>
    /// <param name="pasta">Pasta de origem dos scripts.</param>
    /// <param name="saida">Pasta de saída dos pacotes gerados.</param>
    /// <param name="silencioso">Indica se o modo silencioso está ativado.</param>
    /// <param name="arquivosGerados">Lista de arquivos gerados durante o empacotamento.</param>
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

    /// <summary>
    /// Renomeia os arquivos de acordo com o padrão definido.
    /// </summary>
    /// <param name="arquivos">
    /// Lista de arquivos a serem renomeados.
    /// </param>
    /// <param name="silencioso">
    /// Indica se o modo silencioso está ativado.
    /// </param>
    /// <returns>
    /// Lista de tuplas contendo o nome antigo e o novo nome dos arquivos renomeados.
    /// </returns>
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
                fileSystem.File.Move(arquivo, novoCaminho, overwrite: true);
            }
            catch (Exception ex)
            {
                console.MarkupLineInterpolated($"[red][[ERROR]] Erro ao renomear arquivo {arquivo} para {novoCaminho}: {ex.Message}[/]");

                throw;
            }

            renomeados.Add((arquivo, novoCaminho));

            if (!silencioso)
                console.MarkupLineInterpolated($"[blue][[INFO]] Arquivo renomeado: {nome} » {novoNome}[/]");
        }

        return renomeados;
    }

    /// <summary>
    /// Empacota os scripts diretamente em um arquivo zip.
    /// Cria um pacote zip contendo todos os arquivos de script encontrados na pasta de origem.
    /// </summary>
    /// <param name="pastaOrigem">Pasta de origem dos scripts.</param>
    /// <param name="destinoZip">Caminho do arquivo zip de destino.</param>
    /// <param name="silencioso">Indica se o modo silencioso está ativado.</param>
    private void EmpacotarScriptsDireto(string pastaOrigem, string destinoZip, bool silencioso)
    {
        var arquivos = ListarArquivosComRelativo(pastaOrigem).ToList();

        if (arquivos.Count <= 1) // Só tem o config.json
        {
            if (!silencioso)
                console.MarkupLineInterpolated($"[yellow][[WARN]] Nenhum arquivo de script encontrado em: {pastaOrigem}[/]");

            return;
        }

        if (fileSystem.File.Exists(destinoZip))
            fileSystem.File.Delete(destinoZip);

        zipService.CompactarZip(pastaOrigem, arquivos, destinoZip);

        if (!silencioso)
            console.MarkupLineInterpolated($"[green][[SUCCESS]] Pacote gerado: {destinoZip}[/]");
    }

    /// <summary>
    /// Lista as subpastas válidas dentro de uma pasta.
    /// Uma subpasta é considerada válida se seu nome corresponde ao padrão definido e contém um arquivo config.json.
    /// </summary>
    /// <param name="pasta">Pasta a ser verificada.</param>
    /// <returns>Lista de subpastas válidas.</returns>
    internal IEnumerable<string> ListarSubpastasValidas(string pasta)
    {
        var regex = RegexPastasValidas();

        return fileSystem.Directory
            .EnumerateDirectories(pasta)
            .Where(subpasta => regex.IsMatch(fileSystem.Path.GetFileName(subpasta))
                && TemConfigJson(subpasta));
    }

    /// <summary>
    /// Lista os arquivos dentro de uma pasta, retornando o caminho completo e o caminho relativo.
    /// Inclui todos os arquivos .sql e .migration recursivamente, mantendo a estrutura relativa.
    /// </summary>
    /// <param name="pasta">Pasta a ser verificada.</param>
    /// <returns>Lista de arquivos com seus caminhos completos e relativos.</returns>
    internal IEnumerable<(string CaminhoCompleto, string CaminhoNoZip)> ListarArquivosComRelativo(string pasta)
    {
        // Inclui todos os arquivos .sql e .migration recursivamente, mantendo estrutura relativa
        var arquivos = fileSystem.Directory
            .EnumerateFiles(pasta, "*.sql", SearchOption.AllDirectories)
            .Concat(fileSystem.Directory.EnumerateFiles(pasta, "*.migration", SearchOption.AllDirectories))
            .ToList();

        // Sempre inclui o config.json da raiz
        var config = Path.Combine(pasta, "config.json");

        if (fileSystem.File.Exists(config))
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
