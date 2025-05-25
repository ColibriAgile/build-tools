using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Resumos;

/// <summary>
/// Exibe um resumo dos pacotes gerados em formato Markdown.
/// </summary>
/// <param name="console">
/// A instância do console ANSI utilizada para exibir o resumo.
/// </param>
/// <param name="resultado">
/// Resultado do empacotamento, incluindo arquivos gerados e renomeados.
/// </param>
public sealed class ResumoScriptsMarkdown(IAnsiConsole console, EmpacotamentoScriptResultado resultado) : IResumo
{
    /// <inheritdoc/>
    public void ExibirRelatorio()
    {
        console.WriteLine("\n---");
        console.WriteLine("## Resumo dos pacotes gerados\n");
        console.WriteLine("### Arquivos gerados:");

        foreach (var arq in resultado.ArquivosGerados)
            console.WriteLine($"- `{arq}`");

        var listaArquivosRenomeados = resultado.ArquivosRenomeados.ToList();

        if (listaArquivosRenomeados.Count != 0)
        {
            console.WriteLine("\n### Arquivos renomeados:");

            foreach (var (antigo, novo) in listaArquivosRenomeados)
                console.WriteLine($"- `{antigo}` » `{novo}`");
        }

        console.WriteLine("\n---");
    }
}
