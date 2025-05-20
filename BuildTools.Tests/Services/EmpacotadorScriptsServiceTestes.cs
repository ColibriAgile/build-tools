using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using BuildTools.Services;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para EmpacotadorScriptsService.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EmpacotadorScriptsServiceTestes
{
    private static EmpacotadorScriptsService CriarService(MockFileSystem fs)
        => new(fs);

    [Fact]
    public void TemConfigJson_ArquivoValido_DeveRetornarTrue()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\config.json", new MockFileData("{\"a\":1}") }
        });

        var service = CriarService(fs);

        // Act
        var resultado = service.TemConfigJson(@"c:/pasta");

        // Assert
        resultado.ShouldBeTrue();
    }

    [Fact]
    public void TemConfigJson_ArquivoInvalido_DeveLancarExcecao()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\config.json", new MockFileData("{invalido}") }
        });

        var service = CriarService(fs);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.TemConfigJson(@"c:/pasta"));
    }

    [Fact]
    public void TemConfigJson_ArquivoNaoExiste_DeveRetornarFalse()
    {
        // Arrange
        var fs = new MockFileSystem();
        var service = CriarService(fs);

        // Act
        var resultado = service.TemConfigJson(@"c:/pasta");

        // Assert
        resultado.ShouldBeFalse();
    }

    [Fact]
    public void ListarSubpastasValidas_DeveRetornarApenasPastasComDoisDigitosEConfigValido()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\base\01abc\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\base\02\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\base\xx\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\base\03semconfig\qualquer.txt", new MockFileData("") }
        });

        fs.AddDirectory(@"c:\base\03semconfig");
        var service = CriarService(fs);

        // Act
        var subpastas = service.ListarSubpastasValidas(@"c:\base").ToList();

        // Assert
        subpastas.ShouldContain(@"c:\base\01abc");
        subpastas.ShouldContain(@"c:\base\02");
        subpastas.ShouldNotContain(@"c:\base\xx");
        subpastas.ShouldNotContain(@"c:\base\03semconfig");
    }

    [Fact]
    public void ListarArquivosComRelativo_DeveRetornarSqlMigrationEConfigJsonComRelativoCorreto()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\um.sql", new MockFileData("select 1;") },
            { @"c:\pasta\dois.migration", new MockFileData("migra") },
            { @"c:\pasta\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\pasta\sub\abc.sql", new MockFileData("select 2;") }
        });

        fs.AddDirectory(@"c:/pasta/sub");
        var service = CriarService(fs);

        // Act
        var arquivos = service.ListarArquivosComRelativo(@"c:/pasta").ToList();

        // Assert
        arquivos.ShouldContain(static x => x.CaminhoNoZip == "um.sql");
        arquivos.ShouldContain(static x => x.CaminhoNoZip == "dois.migration");
        arquivos.ShouldContain(static x => x.CaminhoNoZip == @"sub\abc.sql");
        arquivos.ShouldContain(static x => x.CaminhoNoZip == "config.json");
    }
}
