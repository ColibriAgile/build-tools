using BuildTools.Constants;
using BuildTools.Services;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para o serviço de manipulação de arquivos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ArquivoServiceTestes
{
    private readonly IFileSystem _fileSystem;
    private readonly ArquivoService _service;
    private const string PASTA_TESTE = "C:/teste";

    public ArquivoServiceTestes()
    {
        _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{PASTA_TESTE}/arquivo.cmpkg", new MockFileData("conteudo") },
            { $"{PASTA_TESTE}/Teste_1_2_3.cmpkg", new MockFileData("conteudo") },
            { $"{PASTA_TESTE}/OutroTeste_4_5_6.cmpkg", new MockFileData("conteudo") }
        });

        _service = new ArquivoService(_fileSystem);

        // Limpa variáveis de ambiente que podem afetar os testes
        Environment.SetEnvironmentVariable("QA", null);
        Environment.SetEnvironmentVariable("ALOHA", null);
        Environment.SetEnvironmentVariable("PASTA_QA", null);
    }

    [Fact]
    public void ExcluirComPrefixo_ComPrefixoExistente_DeveExcluirArquivosCorrespondentes()
    {
        // Arrange
        const string PREFIXO = "Teste";

        // Act
        _service.ExcluirComPrefixo(PASTA_TESTE, PREFIXO, EmpacotadorConstantes.EXTENSAO_PACOTE);

        // Assert
        _fileSystem.Directory.GetFiles(PASTA_TESTE, $"{PREFIXO}*{EmpacotadorConstantes.EXTENSAO_PACOTE}")
            .ShouldBeEmpty();

        // O arquivo sem o prefixo deve permanecer
        _fileSystem.File.Exists($"{PASTA_TESTE}/arquivo.cmpkg").ShouldBeTrue();

        // Arquivo com outro prefixo deve permanecer
        _fileSystem.File.Exists($"{PASTA_TESTE}/OutroTeste_4_5_6.cmpkg").ShouldBeTrue();
    }

    [Fact]
    public void ExcluirComPrefixo_SemArquivosCorrespondentes_NaoDeveGerarExcecao()
    {
        // Arrange
        const string PREFIXO_INEXISTENTE = "PrefixoQueNaoExiste";

        // Act & Assert (não deve lançar exceção)
        Should.NotThrow(() => _service.ExcluirComPrefixo(PASTA_TESTE, PREFIXO_INEXISTENTE, EmpacotadorConstantes.EXTENSAO_PACOTE));
    }
}
