using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Resumos;

/// <summary>
/// Classe responsável por exibir o resumo do empacotamento no console.
/// </summary>
/// <param name="console">
/// A instância do console ANSI utilizada para exibir o resumo.
/// </param>
/// <param name="resultado">
/// Resultado do empacotamento, incluindo caminho do pacote e arquivos incluídos.
/// </param>
public sealed class ResumoCmpkgConsole(IAnsiConsole console, EmpacotamentoResultado resultado) : IResumo
{
    /// <inheritdoc/>
    public void ExibirRelatorio()
    {
        console.MarkupLine("[blue]Resumo do Empacotamento[/]");
        console.MarkupLineInterpolated($"[grey]Pasta do pacote gerado:[/] [blue]{Path.GetDirectoryName(resultado.CaminhoPacote).EscapeMarkup()}[/]");
        console.WriteLine();

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

        console.Write(table);
        console.WriteLine();
    }
}
