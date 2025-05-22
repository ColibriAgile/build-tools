using System.CommandLine;
using System.IO.Abstractions;
using Spectre.Console;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BuildTools.Commands;

/// <summary>
/// Comando para empacotar scripts conforme regras do empacotar_scripts.py.
/// </summary>
public sealed class EmpacotarScriptsCommand : Command
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;
    private readonly IEmpacotadorScriptsService _empacotadorScriptsService;

    private readonly Option<string> _pastaOption = new
    (
        aliases: ["--pasta", "-p"],
        description: "Pasta de origem dos scripts"
    )
    {
        IsRequired = true
    };

    private readonly Option<string> _saidaOption = new
    (
        aliases: ["--saida", "-s"],
        description: "Pasta de saída"
    )
    {
        IsRequired = true
    };

    private readonly Option<bool> _padronizarNomesOption = new
    (
        aliases: ["--padronizar_nomes", "-pn"],
        static () => true,
        description: "Padroniza o nome dos arquivos zip gerados conforme regex. (padrão: true)"
    );

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
        IEmpacotadorScriptsService empacotadorScriptsService
    ) : base("empacotar_scripts", "Empacota os scripts sql de uma pasta em um pacote zip para ser incluído em um pacote .cmpkg.")
    {
        AddOption(_pastaOption);
        AddOption(_saidaOption);
        AddOption(_padronizarNomesOption);

        _fileSystem = fileSystem;
        _console = console;
        _empacotadorScriptsService = empacotadorScriptsService;

        this.SetHandler
        (
            Handle,
            _pastaOption,
            _saidaOption,
            _padronizarNomesOption,
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
            if (semCor)
                AnsiConsole.Profile.Capabilities.Ansi = false;

            var resultado = _empacotadorScriptsService.Empacotar(pasta, saida, padronizarNomes, silencioso);

            ExibirResumo(resultado.ArquivosGerados, resultado.ArquivosRenomeados, resumo);
        }
        catch (Exception ex)
        {
            _console.MarkupLineInterpolated($"[red][[ERROR]] {ex.Message}[/]");

            throw;
        }
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

            var pastaNode = new Tree($"[blue]{pasta.EscapeMarkup()}[/]");

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
}
