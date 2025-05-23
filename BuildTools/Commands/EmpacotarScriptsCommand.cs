using System.CommandLine;
using System.IO.Abstractions;
using Spectre.Console;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using BuildTools.Models;
using BuildTools.Resumos;

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

            ExibirResumo(resultado, resumo);
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
    private void ExibirResumo(EmpacotamentoScriptResultado resultado, string? resumo)
    {
        switch (resumo?.ToLowerInvariant())
        {
            case "markdown":
                new ResumoScriptsMarkdown(_console, resultado).ExibirRelatorio();

                break;
            case "console":
                new ResumoScriptsConsole(_console, _fileSystem, resultado).ExibirRelatorio();

                break;
        }
    }
}
