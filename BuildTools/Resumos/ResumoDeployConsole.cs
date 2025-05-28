using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Resumos;

/// <summary>
/// Exibe relatório de deploy no console.
/// </summary>
public sealed class ResumoDeployConsole : IResumo
{
    private readonly IAnsiConsole _console;
    private readonly DeployResultado _resultado;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ResumoDeployConsole"/>.
    /// </summary>
    /// <param name="console">Console para saída de informações.</param>
    /// <param name="resultado">Resultado do deploy.</param>
    public ResumoDeployConsole(IAnsiConsole console, DeployResultado resultado)
    {
        _console = console;
        _resultado = resultado;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public void ExibirRelatorio()
    {
        var table = new Table();
        table.AddColumn("Propriedade");
        table.AddColumn("Valor");

        table.AddRow("Ambiente", _resultado.Ambiente);
        table.AddRow("URL Marketplace", _resultado.UrlMarketplace);
        table.AddRow("Simulado", _resultado.Simulado ? "Sim" : "Não");
        table.AddRow("Tempo de Execução", $"{_resultado.TempoExecucao.TotalSeconds:F1}s");
        table.AddRow("Arquivos Enviados", _resultado.ArquivosEnviados.Count.ToString());
        table.AddRow("Arquivos Ignorados", _resultado.ArquivosIgnorados.Count.ToString());
        table.AddRow("Arquivos com Falha", _resultado.ArquivosFalharam.Count.ToString());

        _console.Write(table);

        ExibirArquivosEnviados();
        ExibirArquivosIgnorados();
        ExibirArquivosComFalha();
    }

    private void ExibirArquivosComFalha()
    {
        if (_resultado.ArquivosFalharam.Count <= 0)
            return;

        _console.WriteLine();
        _console.MarkupLine("[red]Arquivos com falha:[/]");

        foreach (var arquivo in _resultado.ArquivosFalharam)
            _console.MarkupLineInterpolated($"  [red]✗[/] {arquivo.NomeArquivoS3} - {arquivo.MensagemErro}");
    }

    private void ExibirArquivosIgnorados()
    {
        if (_resultado.ArquivosIgnorados.Count <= 0)
            return;

        _console.WriteLine();
        _console.MarkupLine("[yellow]Arquivos ignorados:[/]");

        foreach (var arquivo in _resultado.ArquivosIgnorados)
            _console.MarkupLineInterpolated($"  [yellow]⚠[/] {arquivo.NomeArquivoS3} - {arquivo.MensagemErro}");
    }

    private void ExibirArquivosEnviados()
    {
        if (_resultado.ArquivosEnviados.Count <= 0)
            return;

        _console.WriteLine();
        _console.MarkupLine("[green]Arquivos enviados com sucesso:[/]");

        foreach (var arquivo in _resultado.ArquivosEnviados)
        {
            _console.MarkupLineInterpolated($"  [green]✓[/] {arquivo.NomeArquivoS3} ({arquivo.Manifesto?.Nome} v{arquivo.Manifesto?.Versao})");

            if (!string.IsNullOrEmpty(arquivo.UrlS3))
                _console.MarkupLineInterpolated($"    URL: [blue]{arquivo.UrlS3}[/]");
        }
    }
}
