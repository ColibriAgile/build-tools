using System.CommandLine;
using BuildTools.Services;
using Spectre.Console;

namespace BuildTools.Commands;

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
    /// <param name="empacotadorService">Serviço para empacotamento de arquivos.</param>
    /// <param name="console">Console para saída de informações.</param>
    public EmpacotarCommand
    (
        IEmpacotadorService empacotadorService,
        IAnsiConsole console
    ) : base("empacotar", "Empacota arquivos de uma pasta conforme manifesto.")
    {
        var pastaOption = new Option<string>
        (
            aliases: ["--pasta", "-p", "/pasta"],
            description: "Pasta de origem dos arquivos"
        )
        {
            IsRequired = true
        };

        var saidaOption = new Option<string>
        (
            aliases: ["--saida", "-s", "/saida"],
            description: "Pasta de saída"
        )
        {
            IsRequired = true
        };

        var senhaOption = new Option<string>
        (
            aliases: ["--senha", "-se", "/senha"],
            description: "Senha do pacote zip (opcional)"
        )
        {
            IsRequired = false
        };

        var versaoOption = new Option<string>
        (
            aliases: ["--versao", "-v", "/versao"],
            description: "Versão do pacote (opcional, sobrescreve a do manifesto)"
        )
        {
            IsRequired = false
        };

        var developOption = new Option<bool>
        (
            aliases: ["--develop", "-d", "/develop"],
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
            developOption
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
    private void Handle(string pasta, string saida, string senha, string versao, bool develop)
    {
        try
        {
            var caminhoPacote = _empacotadorService.Empacotar(pasta, saida, senha, versao, develop);

            _console.MarkupLine($"[green]Empacotamento concluído! Pacote gerado em:[/] [blue]{caminhoPacote}[/]");
        }
        catch (Exception ex)
        {
            _console.MarkupLineInterpolated($"[red]Erro:[/] {ex.Message}");
        }
    }   
}
