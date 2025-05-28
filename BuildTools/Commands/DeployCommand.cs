using System.CommandLine;
using System.Diagnostics;
using BuildTools.Models;
using BuildTools.Resumos;
using BuildTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BuildTools.Commands;

/// <summary>
/// Comando para fazer deploy de pacotes para AWS S3 e notificar o marketplace.
/// </summary>
public sealed class DeployCommand : Command
{
    private readonly IDeployService _deployService;
    private readonly IAnsiConsole _console;

    private readonly Argument<string> _pastaArgument = new
    (
        name: "pasta",
        description: "Pasta contendo os arquivos manifesto.dat e .cmpkg"
    );

    private readonly Option<string> _ambienteOption = new
    (
        aliases: ["--ambiente", "-a"],
        description: "Ambiente de deploy (\"desenvolvimento\", \"producao\" ou \"stage\")"
    )
    {
        IsRequired = false
    };

    private readonly Option<string> _marketplaceUrlOption = new
    (
        aliases: ["--mkt-url", "-m"],
        description: "URL do marketplace (opcional, usa padrão do ambiente)"
    )
    {
        IsRequired = false
    };

    private readonly Option<bool> _simuladoOption = new
    (
        aliases: ["--simulado", "-si"],
        description: "Executa em modo simulado sem fazer upload real"
    );

    private readonly Option<bool> _forcarOption = new
    (
        aliases: ["--forcar", "-f"],
        description: "Força o upload mesmo se o arquivo já existir no S3"
    );

    private readonly Option<string> _awsAccessKeyOption = new
    (
        aliases: ["--aws-access-key"],
        description: "AWS Access Key (opcional, usa variável AWS_ACCESS_KEY_ID se não informado)"
    )
    {
        IsRequired = false
    };

    private readonly Option<string> _awsSecretKeyOption = new
    (
        aliases: ["--aws-secret-key"],
        description: "AWS Secret Key (opcional, usa variável AWS_SECRET_ACCESS_KEY se não informado)"
    )
    {
        IsRequired = false
    };

    private readonly Option<string> _awsRegionOption = new
    (
        aliases: ["--aws-region"],
        description: "AWS Region (opcional, usa variável AWS_REGION ou 'us-east-1' se não informado)"
    )
    {
        IsRequired = false
    };

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="DeployCommand"/>.
    /// </summary>
    /// <param name="silenciosoOption">
    /// Se deve executar o comando em modo silencioso, sem mensagens de log.
    /// </param>
    /// <param name="semCorOption">
    /// Se deve executar o comando sem cores.
    /// </param>
    /// <param name="resumoOption">
    /// Se deve imprimir o resumo em markdown ou console ao final do processo.
    /// </param>
    /// <param name="deployService">Serviço para deploy de pacotes.</param>
    /// <param name="console">Console para saída de informações.</param>
    public DeployCommand
    (
        [FromKeyedServices("silencioso")]
        Option<bool> silenciosoOption,
        [FromKeyedServices("semCor")]
        Option<bool> semCorOption,
        [FromKeyedServices("resumo")]
        Option<string> resumoOption,
        IDeployService deployService,
        IAnsiConsole console)
        : base("deploy", "Faz deploy de pacotes .cmpkg para AWS S3 e notifica o marketplace")
    {
        AddArgument(_pastaArgument);
        AddOption(_ambienteOption);
        AddOption(_marketplaceUrlOption);
        AddOption(_simuladoOption);
        AddOption(_forcarOption);
        AddOption(_awsAccessKeyOption);
        AddOption(_awsSecretKeyOption);
        AddOption(_awsRegionOption);

        _deployService = deployService;
        _console = console;

        ConfigurarValidacoes();

        this.SetHandler
        (
            context =>
            {
                var pasta = context.ParseResult.GetValueForArgument(_pastaArgument);
                var ambiente = context.ParseResult.GetValueForOption(_ambienteOption) ?? "desenvolvimento";
                var marketplaceUrl = context.ParseResult.GetValueForOption(_marketplaceUrlOption);
                var simulado = context.ParseResult.GetValueForOption(_simuladoOption);
                var forcar = context.ParseResult.GetValueForOption(_forcarOption);
                var awsAccessKey = context.ParseResult.GetValueForOption(_awsAccessKeyOption);
                var awsSecretKey = context.ParseResult.GetValueForOption(_awsSecretKeyOption);
                var awsRegion = context.ParseResult.GetValueForOption(_awsRegionOption);
                var silencioso = context.ParseResult.GetValueForOption(silenciosoOption);
                var semCor = context.ParseResult.GetValueForOption(semCorOption);
                var resumo = context.ParseResult.GetValueForOption(resumoOption);

                return HandleAsync(pasta, ambiente, marketplaceUrl, simulado, forcar, awsAccessKey, awsSecretKey, awsRegion, silencioso, semCor, resumo);
            }
        );
    }

    /// <summary>
    /// Configura as validações para os parâmetros do comando.
    /// </summary>
    private void ConfigurarValidacoes()
    {
        _ambienteOption.SetDefaultValue("desenvolvimento");

        _ambienteOption.AddValidator(result =>
        {
            var valor = result.GetValueForOption(_ambienteOption);

            if (string.IsNullOrEmpty(valor))
            {
                return;
            }

            var ambientesValidos = new[] { "desenvolvimento", "producao", "stage" };

            if (!ambientesValidos.Contains(valor, StringComparer.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"Ambiente '{valor}' inválido. Valores permitidos: {string.Join(", ", ambientesValidos)}";
            }
        });
    }

    /// <summary>
    /// Manipula a execução do comando de deploy.
    /// </summary>
    /// <param name="pasta">Pasta contendo os arquivos para deploy.</param>
    /// <param name="ambiente">Ambiente de deploy.</param>
    /// <param name="marketplaceUrl">URL do marketplace.</param>
    /// <param name="simulado">Indica se é uma execução simulada.</param>
    /// <param name="forcar">Força o upload mesmo se o arquivo já existir.</param>
    /// <param name="awsAccessKey">AWS Access Key.</param>
    /// <param name="awsSecretKey">AWS Secret Key.</param>
    /// <param name="awsRegion">AWS Region.</param>
    /// <param name="silencioso">Indica se a saída deve ser silenciosa.</param>
    /// <param name="semCor">Indica se a saída deve ser sem cor.</param>
    /// <param name="resumo">Indica se deve exibir um resumo ao final.</param>
    private async Task HandleAsync
    (
        string pasta,
        string ambiente,
        string? marketplaceUrl,
        bool simulado,
        bool forcar,
        string? awsAccessKey,
        string? awsSecretKey,
        string? awsRegion,
        bool silencioso,
        bool semCor,
        string? resumo
    )
    {
        if (semCor)
            _console.Profile.Capabilities.Ansi = false;

        var sw = Stopwatch.StartNew();

        try
        {
            if (!silencioso)
            {
                var modoTexto = simulado ? " (SIMULADO)" : string.Empty;
                _console.MarkupLineInterpolated($"[blue][[INFO]] Iniciando deploy para ambiente '{ambiente}'{modoTexto}...[/]");
            }

            var resultado = await _deployService.ExecutarDeployAsync
            (
                pasta,
                ambiente,
                marketplaceUrl,
                simulado,
                forcar,
                awsAccessKey,
                awsSecretKey,
                awsRegion
            ).ConfigureAwait(false);

            sw.Stop();

            if (!silencioso)
            {
                var totalArquivos = resultado.ArquivosEnviados.Count + resultado.ArquivosIgnorados.Count + resultado.ArquivosFalharam.Count;
                var tempoExecucao = sw.Elapsed.TotalSeconds;

                _console.MarkupLineInterpolated($"[green][[SUCCESS]] Deploy concluído em {tempoExecucao:N1}s![/]");
                _console.MarkupLineInterpolated($"[green]Arquivos processados: {totalArquivos} | Enviados: {resultado.ArquivosEnviados.Count} | Ignorados: {resultado.ArquivosIgnorados.Count} | Falhas: {resultado.ArquivosFalharam.Count}[/]");

                if (resultado.ArquivosFalharam.Count > 0)
                    _console.MarkupLine("[yellow][[WARN]] Alguns arquivos falharam no deploy. Verifique o resumo para detalhes.[/]");
            }

            ExibirResumo(resultado, resumo);
        }
        catch (Exception ex)
        {
            _console.MarkupLineInterpolated($"[red][[ERROR]] {ex.Message}[/]");

            throw;
        }
    }

    /// <summary>
    /// Exibe o resumo do deploy de acordo com a opção especificada.
    /// Se a opção for "nenhum", não exibe nada.
    /// </summary>
    /// <param name="resultado">
    /// Resultado do deploy, incluindo arquivos processados e estatísticas.
    /// </param>
    /// <param name="resumo">
    /// Tipo de resumo a ser exibido. Pode ser "nenhum", "console" ou "markdown".
    /// </param>
    private void ExibirResumo(DeployResultado resultado, string? resumo)
    {
        switch (resumo?.ToLowerInvariant())
        {
            case "console":
                new ResumoDeployConsole(_console, resultado).ExibirRelatorio();

                break;
            case "markdown":
                new ResumoDeployMarkdown(_console, resultado).ExibirRelatorio();

                break;
        }
    }
}
