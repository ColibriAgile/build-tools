using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Services;

using System.IO.Abstractions;

/// <inheritdoc cref="IEmpacotadorService"/>
public sealed class EmpacotadorService : IEmpacotadorService
{
    private readonly IZipService _zipService;
    private readonly IManifestoService _manifestoService;
    private readonly IManifestoGeradorService _manifestoGeradorService;
    private readonly IArquivoService _arquivoService;
    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;

    public EmpacotadorService
    (
        IZipService zipService,
        IManifestoService manifestoService,
        IManifestoGeradorService manifestoGeradorService,
        IArquivoService arquivoService,
        IAnsiConsole console,
        IFileSystem fileSystem
    )
    {
        _zipService = zipService;
        _manifestoService = manifestoService;
        _manifestoGeradorService = manifestoGeradorService;
        _arquivoService = arquivoService;
        _console = console;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public EmpacotamentoResultado Empacotar(string pasta, string pastaSaida, string senha = "", string? versao = null, bool develop = false)
    {
        var manifestoOriginal = _manifestoService.LerManifesto(pasta);

        if (!string.IsNullOrWhiteSpace(versao))
            manifestoOriginal.Versao = versao;

        manifestoOriginal.Extras ??= new();
        manifestoOriginal.Extras["develop"] = develop;

        // Gera o manifesto expandido e salva
        var manifestoExpandido = _manifestoGeradorService.GerarManifestoExpandido(pasta, manifestoOriginal);
        _manifestoService.SalvarManifesto(pasta, manifestoExpandido);

        // Pega a lista de arquivos do manifesto, ignorando outros arquivos da pasta
        var arquivos = manifestoExpandido.Arquivos
            .Select(static a => a.Nome)
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .Select(static n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var prefixo = manifestoExpandido.Nome.Replace(" ", string.Empty) + "_";
        var nomeCmpkg = prefixo + manifestoExpandido.Versao.Replace(" ", string.Empty).Replace(".", "_") + Constants.EmpacotadorConstantes.EXTENSAO_PACOTE;
        var caminhoSaida = Path.Combine(pastaSaida.TrimEnd('\\'), nomeCmpkg);
        _arquivoService.ExcluirComPrefixo(pastaSaida, prefixo);
        _zipService.CompactarZip(pasta, arquivos, caminhoSaida, senha);

        // Verifica arquivos não incluídos no pacote
        var todosArquivos = _fileSystem.Directory.GetFiles(pasta)
            .Select(Path.GetFileName)
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var arquivosIncluidos = arquivos.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var arquivosNaoIncluidos = todosArquivos.Except(["manifesto.server", ..arquivosIncluidos]).ToList();

        if (arquivosNaoIncluidos.Count > 0)
            _console.MarkupLineInterpolated($"[yellow][[WARN]] Os seguintes arquivos não foram incluídos no pacote: {string.Join(", ", arquivosNaoIncluidos)}[/]");

        return new EmpacotamentoResultado(caminhoSaida, arquivos);
    }
}