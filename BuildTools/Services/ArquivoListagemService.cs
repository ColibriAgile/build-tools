// Arquivo: d:\projetos\BuildTools\BuildTools\Services\ArquivoListagemService.cs
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using BuildTools.Constants;
using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço de geração e ordenação da lista de arquivos a serem empacotados.
/// </summary>
/// <inheritdoc cref="IArquivoListagemService"/>
public sealed class ArquivoListagemService : IArquivoListagemService
{
    private readonly IFileSystem _fileSystem;

    private const string ARQ_SCRIPTS = "scripts";
    private const string ARQ_CLIENT = "client";
    private const string ARQ_SERVIDOR = "server";
    private const string ARQ_SHARED = "shared";
    private const string ARQ_PACOTE = "pacote";
    private const string MANIFESTO_SERVER = "manifesto.server";
    private static readonly Regex RE_ARQ_SCRIPT = new(@"_scripts(\d{0,2})(\S+)?\\.zip", RegexOptions.IgnoreCase);

    public ArquivoListagemService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public List<string> ObterArquivos(string pasta, Manifesto manifesto)
    {
        var arquivosDiretorio = _fileSystem.Directory.GetFiles(pasta)
            .Select(_fileSystem.Path.GetFileName)
            .Where(nome => nome != null)
            .ToList()!;

// Removed unused variable `listaZip`.
        var listaAnterior = manifesto.Arquivos.ToList();
        manifesto.Arquivos.Clear();

        foreach (var arq in arquivosDiretorio)
        {
            if (arq!.Equals(EmpacotadorConstantes.MANIFESTO, StringComparison.OrdinalIgnoreCase))
                continue;

            if (arq.Equals(MANIFESTO_SERVER, StringComparison.OrdinalIgnoreCase))
                continue;

            // Busca por match em pattern_nome
            var existente = listaAnterior.FirstOrDefault(a =>
                (!string.IsNullOrEmpty(a.Nome) && string.Equals(a.Nome, arq, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(a.PatternNome) && Regex.IsMatch(arq, a.PatternNome, RegexOptions.IgnoreCase))
            );

            ManifestoArquivo novoArq = existente ?? new ManifestoArquivo { Nome = arq };

            if (novoArq.Destino is null)
                novoArq.Destino = AcharTipo(arq);

            if (string.IsNullOrEmpty(novoArq.Destino) && arq.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                novoArq.Destino = ARQ_CLIENT;

            // Garante que o nome será preenchido para empacotamento
            if (string.IsNullOrEmpty(novoArq.Nome))
                novoArq.Nome = arq;

            manifesto.Arquivos.Add(novoArq);
        }

        // Adiciona arquivos definidos apenas por pattern que não tiveram match
        foreach (var arqPattern in listaAnterior.Where(a => !string.IsNullOrEmpty(a.PatternNome) && string.IsNullOrEmpty(a.Nome)))
        {
            var matches = arquivosDiretorio
                .Where(f => f != null && arqPattern.PatternNome != null && System.Text.RegularExpressions.Regex.IsMatch(f, arqPattern.PatternNome, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
            foreach (var match in matches)
            {
                if (!manifesto.Arquivos.Any(a => a.Nome == match))
                {
                    var novoArq = new ManifestoArquivo
                    {
                        Nome = match,
                        PatternNome = arqPattern.PatternNome,
                        Destino = arqPattern.Destino,
                        Extras = arqPattern.Extras
                    };
                    manifesto.Arquivos.Add(novoArq);
                }
            }
        }

        // Remove _pattern_nome dos arquivos antes de salvar no manifesto final
        foreach (var arquivo in manifesto.Arquivos)
        {
            if (!string.IsNullOrEmpty(arquivo.PatternNome))
            {
                arquivo.PatternNome = null;
            }
        }

        manifesto.Arquivos = manifesto.Arquivos
            .OrderBy(AcharOrdem)
            .ToList();

        // Gera a lista de nomes dos arquivos a empacotar (nome, não caminho completo)
        var arquivosParaEmpacotar = manifesto.Arquivos
            .Select(a => a.Nome)
            .Where(nome => !string.IsNullOrEmpty(nome))
            .Select(nome => nome!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Sempre inclui o manifesto.dat
        if (!arquivosParaEmpacotar.Contains(EmpacotadorConstantes.MANIFESTO, StringComparer.OrdinalIgnoreCase))
        {
            arquivosParaEmpacotar.Insert(0, EmpacotadorConstantes.MANIFESTO);
        }

        return arquivosParaEmpacotar;
    }

    private static string? AcharTipo(string nomeArquivo)
    {
        if (RE_ARQ_SCRIPT.IsMatch(nomeArquivo))
            return ARQ_SCRIPTS;

        return null;
    }

    private static int AcharOrdem(ManifestoArquivo arq)
    {
        return arq.Destino switch
        {
            ARQ_PACOTE => -1000,
            ARQ_SCRIPTS => 0,
            ARQ_SHARED => 1000,
            ARQ_SERVIDOR => 2000,
            ARQ_CLIENT => 3000,
            _ => 5000
        };
    }
}
