using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Services;

using System.IO.Abstractions;

/// <inheritdoc cref="IEmpacotadorService"/>
public sealed class EmpacotadorService
(
    IZipService zipService,
    IManifestoService manifestoService,
    IManifestoGeradorService manifestoGeradorService,
    IArquivoService arquivoService,
    IAnsiConsole console,
    IFileSystem fileSystem
) : IEmpacotadorService
{
    /// <inheritdoc />
    public EmpacotamentoResultado Empacotar(string pasta, string pastaSaida, string senha = "", string? versao = null, bool develop = false)
    {
        var manifestoOriginal = manifestoService.LerManifesto(pasta);

        if (!string.IsNullOrWhiteSpace(versao))
            manifestoOriginal.Versao = versao;

        manifestoOriginal.Extras ??= new();
        manifestoOriginal.Extras["develop"] = develop;

        // Gera o manifesto expandido e salva
        var manifestoExpandido = manifestoGeradorService.GerarManifestoExpandido(pasta, manifestoOriginal);
        manifestoService.SalvarManifesto(pasta, manifestoExpandido);
        manifestoService.SalvarManifesto(pastaSaida, manifestoExpandido);

        // Pega a lista de arquivos do manifesto, ignorando outros arquivos da pasta
        var arquivos = manifestoExpandido.Arquivos
            .Select(static a => a.Nome)
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .Select(static n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nomeCmpkg = ManifestoGeradorService.CriarNomeArquivoCmpkg
        (
            manifestoExpandido.SiglaEmpresa,
            manifestoExpandido.Versao,
            manifestoExpandido.Nome,
            out var prefixo
        );

        var caminhoSaida = Path.Combine(pastaSaida.TrimEnd('\\'), nomeCmpkg);
        arquivoService.ExcluirComPrefixo(pastaSaida, prefixo);
        zipService.CompactarZip(pasta, arquivos, caminhoSaida, senha);

        // Verifica arquivos não incluídos no pacote
        var todosArquivos = fileSystem.Directory.GetFiles(pasta)
            .Select(Path.GetFileName)
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var arquivosIncluidos = arquivos.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var arquivosNaoIncluidos = todosArquivos.Except(["manifesto.server", .. arquivosIncluidos]).ToList();

        if (arquivosNaoIncluidos.Count > 0)
            console.MarkupLineInterpolated($"[yellow][[WARN]] Os seguintes arquivos não foram incluídos no pacote: {string.Join(", ", arquivosNaoIncluidos)}[/]");

        return new EmpacotamentoResultado(caminhoSaida, Path.Combine(pastaSaida, "manifesto.dat"), arquivos);
    }
}