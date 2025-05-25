using System.IO.Abstractions;
using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Resumos;

/// <summary>
/// Classe responsável por exibir o resumo do empacotamento de scripts no console.
/// </summary>
/// <param name="console">
/// A instância do console ANSI utilizada para exibir o resumo.
/// </param>
/// <param name="fileSystem">
/// O sistema de arquivos utilizado para operações de leitura e escrita.
/// </param>
/// <param name="resultado">
/// Resultado do empacotamento, incluindo arquivos gerados e renomeados.
/// </param>
public sealed class ResumoScriptsConsole(IAnsiConsole console, IFileSystem fileSystem, EmpacotamentoScriptResultado resultado) : IResumo
{
    /// <inheritdoc/>
    public void ExibirRelatorio()
    {
        console.MarkupLine("\n[bold yellow]Resumo dos pacotes gerados[/]\n");

        // Cria um dicionário para mapear arquivos renomeados
        var renomeadosDict = resultado.ArquivosRenomeados.ToDictionary
        (
            static x => x.Antigo,
            static x => x.Novo,
            StringComparer.OrdinalIgnoreCase
        );

        // Agrupa arquivos por pasta
        var arquivosPorPasta = resultado.ArquivosGerados
            .GroupBy(static arq => Path.GetDirectoryName(arq)!)
            .OrderBy(static g => g.Key);

        foreach (var grupo in arquivosPorPasta)
        {
            var pasta = string.IsNullOrEmpty(grupo.Key)
                ? "[root]"
                : Path.GetRelativePath(fileSystem.Directory.GetCurrentDirectory(), grupo.Key).EscapeMarkup();

            var pastaNode = new Tree($"[blue]{pasta.EscapeMarkup()}[/]");

            var table = new Table().RoundedBorder();
            table.AddColumn(new TableColumn("Arquivo"));
            table.AddColumn(new TableColumn("Renomeado"));

            foreach (var arq in grupo)
            {
                var nomeOriginal = Path.GetFileName(arq).EscapeMarkup();

                table.AddRow
                (
                    $"[white]{nomeOriginal}[/]",
                    renomeadosDict.TryGetValue(arq, out var novoNome)
                        ? $"[green]{Path.GetFileName(novoNome).EscapeMarkup()}[/]"
                        : string.Empty
                );
            }

            pastaNode.AddNode(table);
            console.Write(pastaNode);
        }

        console.WriteLine();
    }
}
