using System.Text.RegularExpressions;
using BuildTools.Models;
using System.IO.Abstractions;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço responsável por gerar o manifesto expandido (manifesto.dat).
/// </summary>
public sealed class ManifestoGeradorService : IManifestoGeradorService
{
    private readonly IFileSystem _fileSystem;

    public ManifestoGeradorService(IFileSystem fileSystem)
        => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Manifesto GerarManifestoExpandido(string pasta, Manifesto manifestoOriginal)
    {
        var arquivosDiretorio = _fileSystem.Directory.GetFiles(pasta)
            .Select(Path.GetFileName)
            .Where(static nome => !string.IsNullOrWhiteSpace(nome))
            .ToList();

        var arquivosManifesto = new List<ManifestoArquivo>();
        var arquivosJaAssociados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Primeiro associa por nome
        AdicionarArquivosPeloNome(manifestoOriginal, arquivosDiretorio, arquivosJaAssociados, arquivosManifesto);

        // Depois associa por pattern, sem duplicar
        AdicionarArquivosPeloPattern(manifestoOriginal, arquivosDiretorio, arquivosJaAssociados, arquivosManifesto);

        // Garante que manifesto.dat sempre esteja presente e como primeiro
        arquivosManifesto.RemoveAll
        (
            static x => x.Nome != null
                && x.Nome.Equals(Constants.EmpacotadorConstantes.MANIFESTO, StringComparison.OrdinalIgnoreCase)
        );

        arquivosManifesto.Insert(0, new ManifestoArquivo { Nome = Constants.EmpacotadorConstantes.MANIFESTO });

        return new Manifesto
        {
            Nome = manifestoOriginal.Nome,
            Versao = manifestoOriginal.Versao,
            Arquivos = [.. arquivosManifesto],
            Extras = manifestoOriginal.Extras
        };
    }

    private static void AdicionarArquivosPeloPattern
    (
        Manifesto manifestoOriginal,
        List<string?> arquivosDiretorio,
        HashSet<string> arquivosJaAssociados,
        List<ManifestoArquivo> arquivosManifesto
    )
    {
        foreach (var previsto in manifestoOriginal.Arquivos)
        {
            if (string.IsNullOrEmpty(previsto.PatternNome))
                continue;

            var regex = new Regex(previsto.PatternNome, RegexOptions.IgnoreCase);

            var encontrados = arquivosDiretorio
                .Where(arq => !arquivosJaAssociados.Contains(arq!))
                .Where(arq => regex.IsMatch(arq!))
                .ToList();

            if (encontrados.Count == 0)
                throw new InvalidOperationException($"Nenhum arquivo encontrado para o pattern previsto no manifesto: {previsto.PatternNome}");

            foreach (var arq in encontrados)
            {
                arquivosManifesto.Add
                (
                    new ManifestoArquivo
                    {
                        Nome = arq!,
                        Destino = previsto.Destino,
                        PatternNome = null,
                        Extras = previsto.Extras
                    }
                );

                arquivosJaAssociados.Add(arq!);
            }
        }
    }

    private static void AdicionarArquivosPeloNome
    (
        Manifesto manifestoOriginal,
        List<string?> arquivosDiretorio,
        HashSet<string> arquivosJaAssociados,
        List<ManifestoArquivo> arquivosManifesto
    )
    {
        foreach (var previsto in manifestoOriginal.Arquivos.Where(static previsto => !string.IsNullOrEmpty(previsto.Nome)))
        {
            var nomePrevisto = previsto.Nome!;

            if (!arquivosDiretorio.Contains(nomePrevisto, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Arquivo previsto no manifesto não encontrado: {nomePrevisto}");

            if (!arquivosJaAssociados.Contains(nomePrevisto))
            {
                arquivosManifesto.Add
                (
                    new ManifestoArquivo
                    {
                        Nome = nomePrevisto,
                        Destino = previsto.Destino,
                        PatternNome = null,
                        Extras = previsto.Extras
                    }
                );
            }

            arquivosJaAssociados.Add(nomePrevisto);
        }
    }
}
