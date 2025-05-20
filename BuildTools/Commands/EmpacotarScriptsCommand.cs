using System.CommandLine;
using System.IO.Abstractions;
using System.IO.Compression;
using Spectre.Console;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BuildTools.Commands;

/// <summary>
/// Comando para empacotar scripts conforme regras do empacotar_scripts.py.
/// </summary>
public sealed partial class EmpacotarScriptsCommand : Command
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;
    private readonly EmpacotadorScriptsService _empacotadorScriptsService;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="EmpacotarScriptsCommand"/>.
    /// </summary>
    /// <param name="resumoOption">
    /// Se deve imprimir o resumo em markdown ao final do processo.
    /// </param>
    /// <param name="silenciosoOption">
    /// Se deve executar o comando em modo silencioso, sem mensagens de log.
    /// </param>
    /// <param name="semCorOption">
    /// Se deve executar o comando sem cores.
    /// </param>
    /// <param name="fileSystem">Abstração do sistema de arquivos.</param>
    /// <param name="console">Console para saída formatada.</param>
    /// <param name="empacotadorScriptsService">Serviço para empacotamento de scripts.</param>
    public EmpacotarScriptsCommand
    (
        [FromKeyedServices("silencioso")]
        Option<bool> silenciosoOption,
        [FromKeyedServices("semCor")]
        Option<bool> semCorOption,
        [FromKeyedServices("resumo")]
        Option<bool> resumoOption,
        IFileSystem fileSystem,
        IAnsiConsole console,
        EmpacotadorScriptsService empacotadorScriptsService
    ) : base("empacotar_scripts", "Empacota scripts de banco de dados para sistemas Colibri.")
    {
        var pastaOption = new Option<string>
        (
            aliases: ["--pasta", "-p"],
            description: "Pasta de origem dos scripts"
        )
        {
            IsRequired = true
        };

        var saidaOption = new Option<string>(
            aliases: ["--saida", "-s"],
            description: "Pasta de saída"
        )
        {
            IsRequired = true
        };

        var padronizarNomesOption = new Option<bool>
        (
            aliases: ["--padronizar_nomes", "-pn"],
            static () => true,
            description: "Padroniza o nome dos arquivos zip gerados conforme regex. (padrão: true)"
        );

        AddOption(pastaOption);
        AddOption(saidaOption);
        AddOption(padronizarNomesOption);

        _fileSystem = fileSystem;
        _console = console;
        _empacotadorScriptsService = empacotadorScriptsService;

        this.SetHandler
        (
            Handle,
            pastaOption,
            saidaOption,
            padronizarNomesOption,
            silenciosoOption,
            semCorOption,
            resumoOption
        );
    }

    private void Handle
    (
        string pasta,
        string saida,
        bool padronizarNomes,
        bool silencioso,
        bool semCor,
        bool resumo
    )
    {
        ConfigurarConsoleSemCor(semCor);

        if (!_fileSystem.Directory.Exists(pasta))
        {
            _console.MarkupLine($"[red][[ERROR]] A pasta de origem não existe: {pasta}[/]");
            Environment.Exit(1);

            return;
        }

        CriarPastaSaidaSeNecessario(saida, silencioso);

        var arquivosGerados = new List<string>();
        var arquivosRenomeados = new List<(string Antigo, string Novo)>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sucesso = true;

        try
        {
            if (!silencioso)
                _console.MarkupLine("[blue][[INFO]] Iniciando empacotamento...[/]");

            ProcessarEmpacotamento(pasta, saida, silencioso, arquivosGerados);

            if (padronizarNomes)
                arquivosRenomeados = PadronizarNomesArquivos(arquivosGerados, silencioso);

            sw.Stop();

            if (!silencioso)
                _console.MarkupLineInterpolated($"[green][[SUCCESS]] Todos os pacotes gerados com sucesso em {sw.Elapsed.TotalSeconds:N1}s.[/]");

            if (resumo)
                ExibirResumoMarkdown(arquivosGerados, arquivosRenomeados);
        }
        catch (Exception ex)
        {
            sucesso = false;
            _console.MarkupLineInterpolated($"[red][[ERROR]] {ex.Message}[/]");
            Environment.Exit(1);
        }

        if (!sucesso)
            Environment.Exit(1);
    }

    private void ProcessarEmpacotamento(string pasta, string saida, bool silencioso, List<string> arquivosGerados)
    {
        if (_empacotadorScriptsService.TemConfigJson(pasta))
        {
            var destinoZip = _fileSystem.Path.Combine(saida, "_scripts.zip");
            EmpacotarScriptsDireto(pasta, destinoZip, silencioso);
            arquivosGerados.Add(destinoZip);

            return;
        }

        foreach (var subpasta in _empacotadorScriptsService.ListarSubpastasValidas(pasta))
        {
            var nome = _fileSystem.Path.GetFileName(subpasta);
            var destinoZip = _fileSystem.Path.Combine(saida, $"_scripts{nome}.zip");
            EmpacotarScriptsDireto(subpasta, destinoZip, silencioso);
            arquivosGerados.Add(destinoZip);
        }
    }

    private static void ConfigurarConsoleSemCor(bool semCor)
    {
        if (semCor)
            AnsiConsole.Profile.Capabilities.Ansi = false;
    }

    private void CriarPastaSaidaSeNecessario(string saida, bool silencioso)
    {
        if (_fileSystem.Directory.Exists(saida))
            return;

        _fileSystem.Directory.CreateDirectory(saida);

        if (!silencioso)
            _console.MarkupLineInterpolated($"[yellow][[INFO]] Pasta de saída criada: {saida}[/]");
    }

    private void EmpacotarScriptsDireto(string pastaOrigem, string destinoZip, bool silencioso)
    {
        var arquivos = _empacotadorScriptsService.ListarArquivosComRelativo(pastaOrigem).ToList();

        if (arquivos.Count == 0)
        {
            if (!silencioso)
                _console.MarkupLineInterpolated($"[yellow][[WARN]] Nenhum arquivo de script encontrado em: {pastaOrigem}[/]");

            return;
        }

        if (_fileSystem.File.Exists(destinoZip))
            _fileSystem.File.Delete(destinoZip);

        using var zip = ZipFile.Open(destinoZip, ZipArchiveMode.Create);

        foreach (var (arquivo, relativo) in arquivos)
            zip.CreateEntryFromFile(arquivo, relativo);

        if (!silencioso)
            _console.MarkupLineInterpolated($"[green][[SUCCESS]] Pacote gerado: {destinoZip}[/]");
    }

    private List<(string Antigo, string Novo)> PadronizarNomesArquivos(IEnumerable<string> arquivos, bool silencioso)
    {
        var regex = RegexPadronizaNomes();
        var renomeados = new List<(string, string)>();

        foreach (var arquivo in arquivos)
        {
            var nome = _fileSystem.Path.GetFileName(arquivo);
            var pasta = _fileSystem.Path.GetDirectoryName(arquivo) ?? string.Empty;
            var match = regex.Match(nome);

            if (!match.Success)
                continue;

            var parte2 = match.Groups[2].Value;
            var parte3 = match.Groups[3].Value;
            var novoNome = $"scripts{parte2}{parte3}.zip";
            var novoCaminho = _fileSystem.Path.Combine(pasta, novoNome);

            try
            {
                _fileSystem.File.Move(arquivo, novoCaminho, overwrite: true);
            }
            catch (Exception ex)
            {
                _console.MarkupLineInterpolated($"[red][[ERROR]] Erro ao renomear arquivo {arquivo} para {novoCaminho}: {ex.Message}[/]");

                Environment.Exit(1);
            }

            renomeados.Add((arquivo, novoCaminho));

            if (!silencioso)
                _console.MarkupLineInterpolated($"[blue][[INFO]] Arquivo renomeado: {nome} » {novoNome}[/]");
        }

        return renomeados;
    }

    private void ExibirResumoMarkdown(IEnumerable<string> arquivosGerados, IEnumerable<(string Antigo, string Novo)> renomeados)
    {
        _console.WriteLine("\n---");
        _console.WriteLine("## Resumo dos pacotes gerados\n");
        _console.WriteLine("### Arquivos gerados:");

        foreach (var arq in arquivosGerados)
            _console.WriteLine($"- `{arq}`");

        var listaArquivosRenomeados = renomeados.ToList();

        if (listaArquivosRenomeados.Count != 0)
        {
            _console.WriteLine("\n### Arquivos renomeados:");

            foreach (var (antigo, novo) in listaArquivosRenomeados)
                _console.WriteLine($"- `{antigo}` » `{novo}`");
        }

        _console.WriteLine("\n---");
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^_scripts(\d{0,2})(\S+)?\.zip$")]
    private static partial System.Text.RegularExpressions.Regex RegexPadronizaNomes();
}
