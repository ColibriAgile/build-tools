using BuildTools.Models;
using BuildTools.Services;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para EmpacotadorService.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EmpacotadorServiceTestes
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IZipService _zipService = Substitute.For<IZipService>();
    private readonly IManifestoService _manifestoService = Substitute.For<IManifestoService>();
    private readonly IArquivoListagemService _arquivoListagemService = Substitute.For<IArquivoListagemService>();
    private readonly IArquivoService _arquivoService = Substitute.For<IArquivoService>();
    private readonly EmpacotadorService _service;

    public EmpacotadorServiceTestes()
        => _service = new EmpacotadorService(_fileSystem, _zipService, _manifestoService, _arquivoListagemService, _arquivoService);

    [Fact]
    public void Empacotar_DeveChamarTodosServicosENomearArquivoCorretamente()
    {
        // Arrange
        const string PASTA = "C:/origem";
        const string PASTA_SAIDA = "C:/saida";
        var manifesto = new Manifesto { Nome = "Meu Pacote", Versao = "1.2.3", Arquivos = [] };
        var arquivos = new List<string> { "manifesto.dat", "arquivo1.txt" };
        _manifestoService.LerManifesto(PASTA).Returns(manifesto);
        _arquivoListagemService.ObterArquivos(PASTA, manifesto).Returns(arquivos);
        _fileSystem.Path.Combine(PASTA_SAIDA, "MeuPacote_1_2_3.cmpkg").Returns("C:/saida/MeuPacote_1_2_3.cmpkg");

        // Act
        var caminho = _service.Empacotar(PASTA, PASTA_SAIDA, senha: "senha123", versao: "1.2.3", develop: true);

        // Assert
        caminho.ShouldBe("C:/saida/MeuPacote_1_2_3.cmpkg");
        _manifestoService.Received(1).LerManifesto(PASTA);
        _arquivoListagemService.Received(1).ObterArquivos(PASTA, manifesto);
        _manifestoService.Received(1).SalvarManifesto(PASTA, manifesto);
        _arquivoService.Received(1).ExcluirComPrefixo(PASTA_SAIDA, "MeuPacote_", ".cmpkg");
        _zipService.Received(1).CompactarZip(PASTA, arquivos, "C:/saida/MeuPacote_1_2_3.cmpkg", "senha123");
        manifesto.Extras.ShouldNotBeNull();
        manifesto.Extras!["develop"].ShouldBe(true);
        manifesto.Versao.ShouldBe("1.2.3");
    }

    [Fact]
    public void Empacotar_QuandoVersaoNaoInformada_DeveManterVersaoDoManifesto()
    {
        // Arrange
        const string PASTA = "C:/origem";
        const string PASTA_SAIDA = "C:/saida";
        var manifesto = new Manifesto { Nome = "Pacote", Versao = "9.9.9", Arquivos = [] };
        var arquivos = new List<string> { "manifesto.dat" };
        _manifestoService.LerManifesto(PASTA).Returns(manifesto);
        _arquivoListagemService.ObterArquivos(PASTA, manifesto).Returns(arquivos);
        _fileSystem.Path.Combine(PASTA_SAIDA, "Pacote_9_9_9.cmpkg").Returns("C:/saida/Pacote_9_9_9.cmpkg");

        // Act
        var caminho = _service.Empacotar(PASTA, PASTA_SAIDA);

        // Assert
        caminho.ShouldBe("C:/saida/Pacote_9_9_9.cmpkg");
        manifesto.Versao.ShouldBe("9.9.9");
    }

    [Fact]
    public void Empacotar_SetaDevelopFalseQuandoNaoInformado()
    {
        // Arrange
        const string PASTA = "C:/origem";
        const string PASTA_SAIDA = "C:/saida";
        var manifesto = new Manifesto { Nome = "Pacote", Versao = "1.0.0", Arquivos = [] };
        var arquivos = new List<string> { "manifesto.dat" };
        _manifestoService.LerManifesto(PASTA).Returns(manifesto);
        _arquivoListagemService.ObterArquivos(PASTA, manifesto).Returns(arquivos);
        _fileSystem.Path.Combine(PASTA_SAIDA, "Pacote_1_0_0.cmpkg").Returns("C:/saida/Pacote_1_0_0.cmpkg");

        // Act
        _service.Empacotar(PASTA, PASTA_SAIDA);

        // Assert
        manifesto.Extras.ShouldNotBeNull();
        manifesto.Extras!["develop"].ShouldBe(false);
    }
}
