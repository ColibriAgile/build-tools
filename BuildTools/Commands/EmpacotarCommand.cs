using System.CommandLine;
using BuildTools.Models;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BuildTools.Commands;

using System.Diagnostics;

/// <summary>
/// Comando para empacotar arquivos de uma pasta conforme manifesto.
/// </summary>
public sealed class EmpacotarCommand : Command
{
    private readonly IEmpacotadorService _empacotadorService;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="EmpacotarCommand"/>.
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
    ) : base("empacotar", "[blue]Empacota arquivos de uma pasta para um pacote .cmpkg conforme regras do arquivo manifesto.server.[/]")
    {
        var pastaOption = new Option<string>
        (
            aliases: ["--pasta", "-p"],
            description: "Pasta de origem dos arquivos"
        )
        {
            IsRequired = true
        };

        var saidaOption = new Option<string>
        (
            aliases: ["--saida", "-s"],
            description: "Pasta de saída"
        )
        {
            IsRequired = true
        };

        var senhaOption = new Option<string>
        (
            aliases: ["--senha", "-se"],
            description: "Senha do pacote zip (opcional)"
        )
        {
            IsRequired = false
        };

        var versaoOption = new Option<string>
        (
            aliases: ["--versao", "-v"],
            description: "Versão do pacote (opcional, sobrescreve a do manifesto)"
        )
        {
            IsRequired = false
        };

        var developOption = new Option<bool>
        (
            aliases: ["--develop", "-d"],
            description: "Marca o pacote como versão de desenvolvimento (opcional)"
        );

        AddOption(pastaOption);
        AddOption(saidaOption);
        AddOption(senhaOption);
        AddOption(versaoOption);
        AddOption(developOption);

        _empacotadorService = empacotadorService;
        _console = console;

        this.SetHandler
        (
            Handle,
            pastaOption,
            saidaOption,
            senhaOption,
            versaoOption,
            developOption,
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
                ExibirResumoConsole(resultado);

                break;
            case "markdown":
                ExibirResumoMarkdown(resultado);

                break;
        }
    }

    /// <summary>
    /// Exibe o resumo do empacotamento no console usando os recursos de formatação de AnsiConsole do Spectre.Console.
    /// </summary>
    /// <param name="resultado">
    /// Resultado do empacotamento, incluindo caminho do pacote e arquivos incluídos.
    /// </param>
    private void ExibirResumoConsole(EmpacotamentoResultado resultado)
    {
        _console.MarkupLine("[blue]Resumo do Empacotamento[/]");
        _console.MarkupLine($"[grey]Pasta do pacote gerado:[/] [blue]{Path.GetDirectoryName(resultado.CaminhoPacote).EscapeMarkup()}[/]");
        _console.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Pacote Gerado")
            .AddColumn("Arquivos Incluídos");

        var nomePacote = Path.GetFileName(resultado.CaminhoPacote);
        var arquivos = string.Join("\n", resultado.ArquivosIncluidos.Select(static a => $"[grey]{Path.GetFileName(a).EscapeMarkup()}[/]"));

        table.AddRow
        (
            $"[blue]{nomePacote.EscapeMarkup()}[/]",
            arquivos
        );

        _console.Write(table);
        _console.WriteLine();
    }

    /// <summary>
    /// Exibe o resumo do empacotamento em formato Markdown.
    /// </summary>
    /// <param name="resultado">
    /// Resultado do empacotamento, incluindo caminho do pacote e arquivos incluídos.
    /// </param>
    private void ExibirResumoMarkdown(EmpacotamentoResultado resultado)
    {
        _console.WriteLine("\n---");
        _console.WriteLine("## Resumo do empacotamento\n");
        _console.WriteLine($"- Pacote gerado: `{resultado.CaminhoPacote}`");
        _console.WriteLine("\n### Arquivos incluídos no pacote:");

        foreach (var arq in resultado.ArquivosIncluidos)
            _console.WriteLine($"- `{arq}`");

        _console.WriteLine("\n---");
    }
}
