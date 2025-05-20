using System.IO.Abstractions;

namespace BuildTools.Services;

/// <inheritdoc cref="IEmpacotadorService"/>
public sealed class EmpacotadorService : IEmpacotadorService
{
    private readonly IFileSystem _fileSystem;
    private readonly IZipService _zipService;
    private readonly IManifestoService _manifestoService;
    private readonly IArquivoListagemService _arquivoListagemService;
    private readonly IArquivoService _arquivoService;

    public EmpacotadorService
    (
        IFileSystem fileSystem,
        IZipService zipService,
        IManifestoService manifestoService,
        IArquivoListagemService arquivoListagemService,
        IArquivoService arquivoService
    )
    {
        _fileSystem = fileSystem;
        _zipService = zipService;
        _manifestoService = manifestoService;
        _arquivoListagemService = arquivoListagemService;
        _arquivoService = arquivoService;
    }

    /// <inheritdoc />
    public string Empacotar(string pasta, string pastaSaida, string senha = "", string? versao = null, bool develop = false)
    {
        var manifesto = _manifestoService.LerManifesto(pasta);

        if (!string.IsNullOrWhiteSpace(versao))
            manifesto.Versao = versao;

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

        return caminhoSaida;
    }
}