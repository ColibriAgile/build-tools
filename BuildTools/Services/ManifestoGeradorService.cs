using System.Text.RegularExpressions;
using BuildTools.Models;
using System.IO.Abstractions;
using BuildTools.Constants;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço responsável por gerar o manifesto expandido (manifesto.dat).
/// </summary>
/// <param name="fileSystem">Sistema de arquivos.</param>
/// <inheritdoc cref="IManifestoGeradorService"/>
public sealed class ManifestoGeradorService(IFileSystem fileSystem) : IManifestoGeradorService
{
    /// <inheritdoc />
    public Manifesto GerarManifestoExpandido(string pasta, Manifesto manifestoOriginal)
    {
        var arquivosDiretorio = fileSystem.Directory.GetFiles(pasta)
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

    /// <summary>
    /// Adiciona arquivos ao manifesto com base no pattern definido no manifesto original.
    /// Se o pattern não for encontrado, lança uma exceção.
    /// </summary>
    /// <param name="manifestoOriginal">
    /// Manifesto original a ser expandido.
    /// </param>
    /// <param name="arquivosDiretorio">
    /// Lista de arquivos presentes no diretório.
    /// </param>
    /// <param name="arquivosJaAssociados">
    /// Conjunto de arquivos já associados ao manifesto.
    /// </param>
    /// <param name="arquivosManifesto">
    /// Lista de arquivos do manifesto.
    /// </param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void AdicionarArquivosPeloPattern
    (
        Manifesto manifestoOriginal,
        List<string?> arquivosDiretorio,
        HashSet<string> arquivosJaAssociados,
        List<ManifestoArquivo> arquivosManifesto
    )
    {
        foreach (var previsto in manifestoOriginal.Arquivos
            .Where(static previsto => !string.IsNullOrEmpty(previsto.PatternNome)))
        {
            var regex = new Regex(previsto.PatternNome!, RegexOptions.IgnoreCase);

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

    /// <summary>
    /// Gera o nome do arquivo .cmpkg com base na sigla da empresa, versão e nome do pacote.
    /// </summary>
    /// <param name="siglaEmpresa">
    /// A sigla da empresa responsável pelo pacote. Pode ser nula ou vazia.
    /// </param>
    /// <param name="versao">
    /// A versão do pacote, que será formatada para substituir pontos por underlines.
    /// </param>
    /// <param name="nome">
    /// O nome do pacote, que será convertido para minúsculas e usado no nome do arquivo.
    /// </param>
    /// <param name="prefixo">
    /// O prefixo do nome do arquivo, que inclui a sigla da empresa (se fornecida) e o nome do pacote em minúsculas.
    /// </param>
    /// <returns>
    /// O nome do arquivo .cmpkg formatado, incluindo a sigla da empresa, nome do pacote e versão.
    /// </returns>
    public static string CriarNomeArquivoCmpkg(string? siglaEmpresa, string versao, string nome, out string prefixo)
    {
        var versaoLimpa = SanitizeFileName(versao.Replace(".", "_"));

        var nomeLimpo = SanitizeFileName(nome
            .ToLowerInvariant()
            .Replace(" ", string.Empty));

        prefixo = !string.IsNullOrWhiteSpace(siglaEmpresa)
            ? $"{siglaEmpresa.ToLowerInvariant()}-{nomeLimpo}_"
            : nomeLimpo + '_';

        return $"{prefixo}{versaoLimpa}{EmpacotadorConstantes.EXTENSAO_PACOTE}";
    }

    /// <summary>
    /// Adiciona arquivos ao manifesto com base no nome definido no manifesto original.
    /// Se o arquivo não for encontrado, lança uma exceção.
    /// </summary>
    /// <param name="manifestoOriginal">
    /// Manifesto original a ser expandido.
    /// </param>
    /// <param name="arquivosDiretorio">
    /// Lista de arquivos presentes no diretório.
    /// </param>
    /// <param name="arquivosJaAssociados">
    /// Conjunto de arquivos já associados ao manifesto.
    /// </param>
    /// <param name="arquivosManifesto">
    /// Lista de arquivos do manifesto.
    /// </param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void AdicionarArquivosPeloNome
    (
        Manifesto manifestoOriginal,
        List<string?> arquivosDiretorio,
        HashSet<string> arquivosJaAssociados,
        List<ManifestoArquivo> arquivosManifesto
    )
    {
        foreach (var previsto in manifestoOriginal.Arquivos
            .Where(static previsto => !string.IsNullOrEmpty(previsto.Nome)))
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
