using System.IO.Abstractions;
using Spectre.Console;

namespace BuildTools.Services;

/// <inheritdoc cref="IEmpacotadorService"/>
public sealed class EmpacotadorService : IEmpacotadorService
{
    private readonly IFileSystem _fileSystem;
    private readonly IZipService _zipService;
    private readonly IAnsiConsole _console;
    private readonly IManifestoService _manifestoService;
    private readonly IArquivoListagemService _arquivoListagemService;
    private readonly IArquivoService _arquivoService;
    private readonly IVersaoBaseService _versaoBaseService;

    public EmpacotadorService(
        IFileSystem fileSystem,
        IZipService zipService,
        IAnsiConsole console,
        IManifestoService manifestoService,
        IArquivoListagemService arquivoListagemService,
        IArquivoService arquivoService,
        IVersaoBaseService versaoBaseService)
    {
        _fileSystem = fileSystem;
        _zipService = zipService;
        _console = console;
        _manifestoService = manifestoService;
        _arquivoListagemService = arquivoListagemService;
        _arquivoService = arquivoService;
        _versaoBaseService = versaoBaseService;
    }

    /// <inheritdoc />
    public string Empacotar(string pasta, string pastaSaida, string senha = "", string? versao = null, bool develop = false)
    {
        var manifesto = _manifestoService.LerManifesto(pasta);

        if (!string.IsNullOrWhiteSpace(versao))
        {
            manifesto.Versao = versao;
        }

        // Sempre gera a chave develop
        manifesto.Extras ??= new();
        manifesto.Extras["develop"] = develop;

        var arquivos = _arquivoListagemService.ObterArquivos(pasta, manifesto);

        _manifestoService.SalvarManifesto(pasta, manifesto);

        var prefixo = manifesto.Nome.Replace(" ", string.Empty) + "_";
        var nomeCmpkg = prefixo + manifesto.Versao.Replace(" ", string.Empty).Replace(".", "_") + Constants.EmpacotadorConstantes.EXTENSAO_PACOTE;
        var caminhoSaida = _fileSystem.Path.Combine(pastaSaida, nomeCmpkg);

        _arquivoService.ExcluirComPrefixo(pastaSaida, prefixo, Constants.EmpacotadorConstantes.EXTENSAO_PACOTE);

        _zipService.CompactarZip(pasta, arquivos, caminhoSaida, senha);

        _arquivoService.CopiarParaQa(nomeCmpkg, prefixo, caminhoSaida);

        return caminhoSaida;
    }
}