using BuildTools.Services;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para o serviço de compactação ZipService.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ZipServiceTestes : IDisposable
{
    private readonly string _pastaTeste;
    private readonly string _zipPath;
    private readonly ZipService _service;

    public ZipServiceTestes()
    {
        _pastaTeste = Path.Combine(Path.GetTempPath(), $"ZipServiceTestes_{Guid.NewGuid()}");
        Directory.CreateDirectory(_pastaTeste);
        _zipPath = Path.Combine(_pastaTeste, "saida.zip");
        _service = new ZipService(new System.IO.Abstractions.FileSystem());
    }

    [Fact]
    public void CompactarZip_ComArquivosValidos_DeveCriarZipComTodosArquivos()
    {
        // Arrange
        var arq1 = Path.Combine(_pastaTeste, "arq1.txt");
        var arq2 = Path.Combine(_pastaTeste, "arq2.txt");
        File.WriteAllText(arq1, "conteudo1");
        File.WriteAllText(arq2, "conteudo2");
        var arquivos = new List<string> { "arq1.txt", "arq2.txt" };

        // Act
        _service.CompactarZip(_pastaTeste, arquivos, _zipPath, senha: null);

        // Assert
        File.Exists(_zipPath).ShouldBeTrue();
        using var zipStream = File.OpenRead(_zipPath);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);
        zip.Entries.Count.ShouldBe(2);
        zip.Entries.ShouldContain(static e => e.Name == "arq1.txt");
        zip.Entries.ShouldContain(static e => e.Name == "arq2.txt");
    }

    [Fact]
    public void CompactarZip_ComArquivoInexistente_DeveIgnorarArquivoInexistente()
    {
        // Arrange
        var arq1 = Path.Combine(_pastaTeste, "arq1.txt");
        File.WriteAllText(arq1, "conteudo1");
        var arquivos = new List<string> { "arq1.txt", "naoexiste.txt" };

        // Act
        _service.CompactarZip(_pastaTeste, arquivos, _zipPath, senha: null);

        // Assert
        File.Exists(_zipPath).ShouldBeTrue();
        using var zipStream = File.OpenRead(_zipPath);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);
        zip.Entries.Count.ShouldBe(1);
        zip.Entries[0].Name.ShouldBe("arq1.txt");
    }

    [Fact]
    public void CompactarZip_SemArquivos_DeveCriarZipVazio()
    {
        // Arrange
        var arquivos = new List<string>();

        // Act
        _service.CompactarZip(_pastaTeste, arquivos, _zipPath, senha: null);

        // Assert
        File.Exists(_zipPath).ShouldBeTrue();
        using var zipStream = File.OpenRead(_zipPath);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);
        zip.Entries.Count.ShouldBe(0);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_pastaTeste))
                Directory.Delete(_pastaTeste, true);
        }
        catch
        {
            //Ignore
        }
    }
}
