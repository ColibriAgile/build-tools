using System.CommandLine;
using BuildTools.Models;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BuildTools.Commands;

using System.Diagnostics;
using BuildTools.Resumos;

/// <summary>
/// Comando para empacotar arquivos de uma pasta conforme manifesto.
/// </summary>
public sealed class EmpacotarCommand : Command
{
    private readonly IEmpacotadorService _empacotadorService;
    private readonly IAnsiConsole _console;

    private readonly Option<string> _pastaOption = new
    (
        aliases: ["--pasta", "-p"],
        description: "Pasta de origem dos arquivos"
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

    private readonly Option<string> _senhaOption = new
    (
        aliases: ["--senha", "-se"],
        description: "Senha do pacote zip (opcional)"
    )
    {
        IsRequired = false
    };

    private readonly Option<string> _versaoOption = new
    (
        aliases: ["--versao", "-v"],
        description: "Versão do pacote (opcional, sobrescreve a do manifesto)"
    )
    {
        IsRequired = false
    };

    private readonly Option<bool> _developOption = new
    (
        aliases: ["--develop", "-d"],
        description: "Marca o pacote como versão de desenvolvimento (opcional)"
    );

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="EmpacotarCommand"/>.
    /// </summary>
    /// <param name="silenciosoOption">
    /// Se deve executar o comando em modo silencioso, sem mensagens de log.
    /// </param>
    /// <param name="semCorOption">
    /// Se deve executar o comando sem cores.
    /// </param>
    /// <param name="resumoOption">
    /// Se deve imprimir o resumo em markdown ao final do processo.
    /// </param>
    /// <param name="empacotadorService">Serviço para empacotamento de arquivos.</param>
    /// <param name="console">Console para saída de informações.</param>
    public EmpacotarCommand
    (
        [FromKeyedServices("silencioso")]
        Option<bool> silenciosoOption,
        [FromKeyedServices("semCor")]
        Option<bool> semCorOption,
        [FromKeyedServices("resumo")]
        Option<string> resumoOption,
        IEmpacotadorService empacotadorService,
        IAnsiConsole console
    ) : base("empacotar", "Empacota arquivos de uma pasta para um pacote .cmpkg conforme regras do arquivo manifesto.server.")
    {
        AddOption(_pastaOption);
        AddOption(_saidaOption);
        AddOption(_senhaOption);
        AddOption(_versaoOption);
        AddOption(_developOption);

        _empacotadorService = empacotadorService;
        _console = console;

        this.SetHandler
        (
            Handle,
            _pastaOption,
            _saidaOption,
            _senhaOption,
            _versaoOption,
            _developOption,
            silenciosoOption,
            semCorOption,
            resumoOption
        );
    }

    /// <summary>
    /// Manipula a execução do comando de empacotamento.
    /// </summary>
    /// <param name="pasta">Pasta de origem dos arquivos.</param>
    /// <param name="saida">Pasta de saída do pacote.</param>
    /// <param name="senha">Senha do pacote zip (opcional).</param>
    /// <param name="versao">Versão do pacote (opcional).</param>
    /// <param name="develop">Indica se o pacote é de desenvolvimento.</param>
    /// <param name="silencioso">Indica se a saída deve ser silenciosa.</param>
    /// <param name="semCor">Indica se a saída deve ser sem cor.</param>
    /// <param name="resumo">Indica se deve exibir um resumo ao final.</param>
    private void Handle
    (
        string pasta,
        string saida,
        string senha,
        string versao,
        bool develop,
        bool silencioso,
        bool semCor,
        string resumo
    )
    {
        if (semCor)
            AnsiConsole.Profile.Capabilities.Ansi = false;

        var sw = Stopwatch.StartNew();

        try
        {
            if (!silencioso)
                _console.MarkupLine("[blue][[INFO]] Iniciando empacotamento...[/]");

            var resultado = _empacotadorService.Empacotar(pasta, saida, senha, versao, develop);
            sw.Stop();

            if (!silencioso)
                _console.MarkupLineInterpolated($"[green][[SUCCESS]] Empacotamento concluído em {sw.Elapsed.TotalSeconds:N1}s! Pacote gerado em: [/] [blue]{resultado.CaminhoPacote}[/]");

            ExibirResumo(resultado, resumo);
        }
        catch (Exception ex)
        {
            _console.MarkupLineInterpolated($"[red][[ERROR]] {ex.Message}[/]");

            throw;
        }
    }

    /// <summary>
    /// Exibe o resumo do empacotamento de acordo com a opção especificada.
    /// Se a opção for "nenhum", não exibe nada.
    /// </summary>
    /// <param name="resultado">
    /// Resultado do empacotamento, incluindo caminho do pacote e arquivos incluídos.
    /// </param>
    /// <param name="resumo">
    /// Tipo de resumo a ser exibido. Pode ser "nenhum", "console" ou "markdown".
    /// </param>
    private void ExibirResumo(EmpacotamentoResultado resultado, string? resumo)
    {
        switch (resumo?.ToLowerInvariant())
        {
            case "console":
                new ResumoCmpkgConsole(_console, resultado).ExibirRelatorio();

                break;
            case "markdown":
                new ResumoCmpkgMarkdown(_console, resultado).ExibirRelatorio();

                break;
        }
    }
}
