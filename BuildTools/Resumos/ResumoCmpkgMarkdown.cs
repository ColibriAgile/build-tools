using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Resumos;

/// <summary>
/// Classe responsável por exibir o resumo do empacotamento no build usando o formato Markdown.
/// </summary>
/// <param name="console">
/// A instância do console ANSI utilizada para exibir o resumo.
/// </param>
/// <param name="resultado">
/// Resultado do empacotamento, incluindo caminho do pacote e arquivos incluídos.
/// </param>
public sealed class ResumoCmpkgMarkdown(IAnsiConsole console, EmpacotamentoResultado resultado) : IResumo
{
    /// <inheritdoc/>
    public void ExibirRelatorio()
    {
        console.WriteLine("\n---");
        console.WriteLine("## Resumo do empacotamento\n");
        console.WriteLine($"- Pacote gerado: `{resultado.CaminhoPacote}`");
        console.WriteLine("\n### Arquivos incluídos no pacote:");

        foreach (var arq in resultado.ArquivosIncluidos)
            console.WriteLine($"- `{arq}`");

        console.WriteLine("\n---");
    }
}