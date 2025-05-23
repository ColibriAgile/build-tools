using BuildTools.Models;
using BuildTools.Services;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para ManifestoService.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ManifestoServiceTestes
{
    private const string PASTA_TESTE = "C:/manifesto";
    private readonly MockFileSystem _fileSystem = new();
    private readonly ManifestoService _service;

    public ManifestoServiceTestes()
        => _service = new ManifestoService(_fileSystem);

    [Fact]
    public void LerManifesto_QuandoExisteManifestoServer_DeveLerComSucesso()
    {
        // Arrange
        var manifesto = new Manifesto { Nome = "Pacote", Versao = "1.0.0", Arquivos = [] };
        var json = JsonSerializer.Serialize(manifesto, new JsonSerializerOptions { WriteIndented = true });
        _fileSystem.AddFile($"{PASTA_TESTE}/manifesto.server", new MockFileData(json));

        // Act
        var lido = _service.LerManifesto(PASTA_TESTE);

        // Assert
        lido.Nome.ShouldBe("Pacote");
        lido.Versao.ShouldBe("1.0.0");
        lido.Arquivos.ShouldBeEmpty();
    }

    [Fact]
    public void LerManifesto_QuandoExisteManifestoLocal_DeveLerComSucesso()
    {
        // Arrange
        var manifesto = new Manifesto { Nome = "PacoteLocal", Versao = "2.0.0", Arquivos = [] };
        var json = JsonSerializer.Serialize(manifesto, new JsonSerializerOptions { WriteIndented = true });
        _fileSystem.AddFile($"{PASTA_TESTE}/manifesto.local", new MockFileData(json));

        // Act
        var lido = _service.LerManifesto(PASTA_TESTE);

        // Assert
        lido.Nome.ShouldBe("PacoteLocal");
        lido.Versao.ShouldBe("2.0.0");
        lido.Arquivos.ShouldBeEmpty();
    }

    [Fact]
    public void LerManifesto_QuandoNaoExisteManifesto_DeveLancarFileNotFoundException()
    {
        // Act & Assert
        Should.Throw<FileNotFoundException>(() => _service.LerManifesto(PASTA_TESTE));
    }

    [Fact]
    public void LerManifesto_QuandoJsonInvalido_DeveLancarInvalidOperationException()
    {
        // Arrange
        _fileSystem.AddFile($"{PASTA_TESTE}/manifesto.server", new MockFileData("{invalido}"));

        // Act & Assert
        Should.Throw<JsonException>(() => _service.LerManifesto(PASTA_TESTE));
    }

    [Fact]
    public void SalvarManifesto_DeveSalvarArquivoManifestoDat()
    {
        // Arrange
        var manifesto = new Manifesto { Nome = "PacoteSalvo", Versao = "3.0.0", Arquivos = [] };

        // Act
        _service.SalvarManifesto(PASTA_TESTE, manifesto);

        // Assert
        _fileSystem.File.Exists($"{PASTA_TESTE}/manifesto.dat").ShouldBeTrue();
        var json = _fileSystem.File.ReadAllText($"{PASTA_TESTE}/manifesto.dat");
        var lido = JsonSerializer.Deserialize<Manifesto>(json);
        lido.ShouldNotBeNull();
        lido.Nome.ShouldBe("PacoteSalvo");
        lido.Versao.ShouldBe("3.0.0");
    }
}
