using System.CommandLine;
using System.IO.Abstractions;
using Spectre.Console;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BuildTools.Commands;

using System.Diagnostics;

/// <summary>
/// Comando para empacotar scripts conforme regras do empacotar_scripts.py.
/// </summary>
public sealed partial class EmpacotarScriptsCommand : Command
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;
    private readonly IEmpacotadorScriptsService _empacotadorScriptsService;
    private readonly IZipService _zipService;

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
    /// <param name="fileSystem">Serviço de manipulação do sistema de arquivos.</param>
    /// <param name="console">Console para saída formatada.</param>
    /// <param name="zipService">
    /// Serviço para manipulação de arquivos zip.
    /// </param>
    /// <param name="empacotadorScriptsService">Serviço para empacotamento de scripts.</param>
    public EmpacotarScriptsCommand
    (
        [FromKeyedServices("silencioso")]
        Option<bool> silenciosoOption,
        [FromKeyedServices("semCor")]
        Option<bool> semCorOption,
        [FromKeyedServices("resumo")]
        Option<string> resumoOption,
        IFileSystem fileSystem,
        IAnsiConsole console,
        IZipService zipService,
        IEmpacotadorScriptsService empacotadorScriptsService
    ) : base("empacotar_scripts", "Empacota os scripts sql de uma pasta em um pacote zip para ser incluído em um pacote .cmpkg.")
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
        _zipService = zipService;
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
        string resumo
    )
    {
        try
        {
            ConfigurarConsoleSemCor(semCor);

            if (!_fileSystem.Directory.Exists(pasta))
            {
                _console.MarkupLineInterpolated($"[red][[ERROR]] A pasta de origem não existe: {pasta}[/]");

                throw new DirectoryNotFoundException($"A pasta de origem não existe: {pasta}");
            }

            CriarPastaSaidaSeNecessario(saida, silencioso);

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

            ExibirResumo(arquivosGerados, arquivosRenomeados, resumo);
        }
        catch (Exception ex)
        {
            _console.MarkupLineInterpolated($"[red][[ERROR]] {ex.Message}[/]");

            throw;
        }
    }

    private void ProcessarEmpacotamento(string pasta, string saida, bool silencioso, List<string> arquivosGerados)
    {
        if (_empacotadorScriptsService.TemConfigJson(pasta))
        {
            var destinoZip = Path.Combine(saida, "_scripts.zip");
            EmpacotarScriptsDireto(pasta, destinoZip, silencioso);
            arquivosGerados.Add(destinoZip);

            return;
        }

        foreach (var subpasta in _empacotadorScriptsService.ListarSubpastasValidas(pasta))
        {
            var nome = Path.GetFileName(subpasta);
            var destinoZip = Path.Combine(saida, $"_scripts{nome}.zip");
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

        _zipService.CompactarZip(pastaOrigem, arquivos, destinoZip);

        if (!silencioso)
            _console.MarkupLineInterpolated($"[green][[SUCCESS]] Pacote gerado: {destinoZip}[/]");
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

    /// <summary>
    /// Exibe um resumo dos arquivos gerados e renomeados.
    /// </summary>
    /// <param name="arquivosGerados">
    /// Lista de arquivos gerados.
    /// </param>
    /// <param name="renomeados">Lista de arquivos renomeados, com os nomes antigos e novos.</param>
    /// <param name="resumo">
    /// Tipo de resumo a ser exibido. Pode ser "nenhum", "console" ou "markdown".
    /// </param>
    private void ExibirResumo(IEnumerable<string> arquivosGerados, IEnumerable<(string Antigo, string Novo)> renomeados, string? resumo)
    {
        switch (resumo?.ToLowerInvariant())
        {
            case "markdown":
                ExibirResumoMarkdown(arquivosGerados, renomeados);

                break;
            case "console":
                ExibirResumoConsole(arquivosGerados, renomeados);

                break;
        }
    }

    /// <summary>
    /// Exibe um resumo dos arquivos gerados e renomeados usando os recursos de formatação do Spectre.Console.
    /// </summary>
    /// <param name="arquivosGerados">
    /// Lista de arquivos gerados.
    /// </param>
    /// <param name="renomeados">
    /// Lista de arquivos renomeados, com os nomes antigos e novos.
    /// </param>
    private void ExibirResumoConsole(IEnumerable<string> arquivosGerados, IEnumerable<(string Antigo, string Novo)> renomeados)
    {
        _console.MarkupLine("\n[bold yellow]Resumo dos pacotes gerados[/]\n");

        // Cria um dicionário para mapear arquivos renomeados
        var renomeadosDict = renomeados.ToDictionary
        (
            static x => x.Antigo,
            static x => x.Novo,
            StringComparer.OrdinalIgnoreCase
        );

        // Agrupa arquivos por pasta
        var arquivosPorPasta = arquivosGerados
            .GroupBy(static arq => Path.GetDirectoryName(arq)!)
            .OrderBy(static g => g.Key);

        foreach (var grupo in arquivosPorPasta)
        {
            var pasta = string.IsNullOrEmpty(grupo.Key)
                ? "[root]"
                : Path.GetRelativePath(_fileSystem.Directory.GetCurrentDirectory(), grupo.Key).EscapeMarkup();

            var pastaNode = new Tree($"[blue]{pasta}[/]");

            var table = new Table().RoundedBorder();
            table.AddColumn(new TableColumn("Arquivo"));
            table.AddColumn(new TableColumn("Renomeado"));

            foreach (var arq in grupo)
            {
                var nomeOriginal = Path.GetFileName(arq).EscapeMarkup();

                table.AddRow
                (
                    $"[white]{nomeOriginal}[/]",
                    renomeadosDict.TryGetValue(arq, out var novoNome)
                        ? $"[green]{Path.GetFileName(novoNome).EscapeMarkup()}[/]"
                        : string.Empty
                );
            }

            pastaNode.AddNode(table);
            _console.Write(pastaNode);
        }

        _console.WriteLine();
    }

    /// <summary>
    /// Exibe um resumo dos pacotes gerados em formato Markdown.
    /// </summary>
    /// <param name="arquivosGerados">
    /// Lista de arquivos gerados.
    /// </param>
    /// <param name="renomeados">
    /// Lista de arquivos renomeados, com os nomes antigos e novos.
    /// </param>
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
