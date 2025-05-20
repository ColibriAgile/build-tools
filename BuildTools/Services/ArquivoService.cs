using System.IO.Abstractions;
using Spectre.Console;
using BuildTools.Constants;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço utilitário para manipulação de arquivos e diretórios.
/// </summary>
/// <inheritdoc cref="IArquivoService"/>
public sealed class ArquivoService : IArquivoService
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;
    private const string PASTA_QA = @"d:\Builder\QA";
    private const string VAR_QA = "QA";
    private const string VAR_ALOHA = "ALOHA";
    private const string VAR_PASTA_QA = "PASTA_QA";

    public ArquivoService(IFileSystem fileSystem, IAnsiConsole console)
    {
        _fileSystem = fileSystem;
        _console = console;
    }

    public void ExcluirComPrefixo(string pasta, string prefixo, string extensao)
    {
        var arquivos = _fileSystem.Directory.GetFiles(pasta, $"{prefixo}*{EmpacotadorConstantes.EXTENSAO_PACOTE}");

        foreach (var arquivo in arquivos)
            _fileSystem.File.Delete(arquivo);
    }

    public void CopiarParaQa(string nomeArquivo, string prefixo, string origem)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(VAR_QA), "true", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(Environment.GetEnvironmentVariable(VAR_ALOHA), "true", StringComparison.OrdinalIgnoreCase))
        {
            _console.MarkupLine("[yellow]Arquivo não será gerado na pasta de QA pois ALOHA=true[/]");

            return;
        }

        var pastaQa = Environment.GetEnvironmentVariable(VAR_PASTA_QA) ?? PASTA_QA;

        if (!_fileSystem.Directory.Exists(pastaQa))
        {
            _console.MarkupLineInterpolated($"[red]PASTA_QA não encontrada: {pastaQa}[/]");

            return;
        }

        ExcluirComPrefixo(pastaQa, prefixo, EmpacotadorConstantes.EXTENSAO_PACOTE);
        var destino = _fileSystem.Path.Combine(pastaQa, nomeArquivo);
        _fileSystem.File.Copy(origem, destino, overwrite: true);
        _console.MarkupLineInterpolated($"[green]Arquivo copiado para QA:[/] [blue]{destino}[/]");
    }
}
