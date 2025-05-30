using System.CommandLine;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BuildTools.Commands;

/// <summary>
/// Comando para notificar o marketplace sobre um pacote específico.
/// </summary>
public sealed class NotificarMarketCommand : Command
{
    private readonly IMarketplaceService _marketplaceService;
    private readonly IManifestoService _manifestoService;
    private readonly IAnsiConsole _console;

    private readonly Argument<string> _pastaArgument = new
    (
        name: "pasta",
        description: "Pasta contendo o arquivo manifesto.dat"
    );

    private readonly Option<string> _marketplaceUrlOption = new
    (
        aliases: ["--mkt-url", "-m"],
        description: "URL do marketplace (opcional, usa padrão do ambiente)"
    )
    {
        IsRequired = false
    };

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="NotificarMarketCommand"/>.
    /// </summary>
    /// <param name="silenciosoOption">Opção para execução silenciosa.</param>
    /// <param name="semCorOption">Opção para execução sem cores.</param>    /// <param name="ambienteOption">Opção para seleção de ambiente.</param>
    /// <param name="marketplaceService">Serviço de notificação do marketplace.</param>
    /// <param name="manifestoService">Serviço para leitura de manifestos.</param>
    /// <param name="console">Console para saída de informações.</param>
    public NotificarMarketCommand
    (
        [FromKeyedServices("silencioso")] Option<bool> silenciosoOption,
        [FromKeyedServices("semCor")] Option<bool> semCorOption,
        [FromKeyedServices("ambiente")] Option<string> ambienteOption,
        IMarketplaceService marketplaceService,
        IManifestoService manifestoService,
        IAnsiConsole console
    )
        : base("notificar-market", "Notifica o marketplace sobre um pacote específico")
    {
        _marketplaceService = marketplaceService;
        _manifestoService = manifestoService;
        _console = console;

        AddArgument(_pastaArgument);
        AddOption(ambienteOption);
        AddOption(_marketplaceUrlOption);
        AddOption(semCorOption);
        AddOption(silenciosoOption);

        this.SetHandler
        (
            context =>
            {
                var pasta = context.ParseResult.GetValueForArgument(_pastaArgument);
                var ambiente = context.ParseResult.GetValueForOption(ambienteOption)!;
                var marketplaceUrl = context.ParseResult.GetValueForOption(_marketplaceUrlOption);
                var silencioso = context.ParseResult.GetValueForOption(silenciosoOption);
                var semCor = context.ParseResult.GetValueForOption(semCorOption);

                return HandleAsync(pasta, ambiente, marketplaceUrl, silencioso, semCor);
            }
        );
    }

    /// <summary>
    /// Manipula a execução do comando de notificação do marketplace.
    /// </summary>
    /// <param name="pasta">Pasta contendo o arquivo manifesto.dat.</param>
    /// <param name="ambiente">Ambiente de deploy.</param>
    /// <param name="marketplaceUrl">URL do marketplace.</param>
    /// <param name="silencioso">Indica se a saída deve ser silenciosa.</param>
    /// <param name="semCor">Indica se a saída deve ser sem cor.</param>
    private async Task HandleAsync
    (
        string pasta,
        string ambiente,
        string? marketplaceUrl,
        bool silencioso,
        bool semCor
    )
    {
        if (semCor)
            _console.Profile.Capabilities.Ansi = false;

        try
        {
            if (!silencioso)
                _console.MarkupLineInterpolated($"[blue][[INFO]] Notificando marketplace para ambiente '{ambiente}'...[/]");

            var manifesto = await _manifestoService.LerManifestoDeployAsync(pasta).ConfigureAwait(false);
            var urlMarketplaceFinal = _marketplaceService.ObterUrlMarketplace(ambiente, marketplaceUrl);

            var sucesso = await _marketplaceService.NotificarPacoteAsync(urlMarketplaceFinal, manifesto)
                .ConfigureAwait(false);

            if (!silencioso)
            {
                _console.MarkupLine
                (
                    sucesso
                    ? "[green][[SUCCESS]] Notificação concluída com sucesso![/]"
                    : "[red][[ERROR]] Falha na notificação do marketplace![/]"
                );
            }
        }
        catch (Exception ex)
        {
            _console.MarkupLineInterpolated($"[red][[ERROR]] {ex.Message}[/]");

            throw;
        }
    }
}
