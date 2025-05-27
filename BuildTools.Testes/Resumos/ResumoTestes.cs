using BuildTools.Models;
using BuildTools.Resumos;
using Spectre.Console.Testing;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Testes.Resumos;

/// <summary>
/// Testes para ResumoScriptsMarkdown.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumoScriptsMarkdownTestes
{
    [Fact]
    public void ExibirRelatorio_DeveExibirResumoMarkdownCorreto()
    {
        // Arrange
        var console = new TestConsole();

        var resultado = new EmpacotamentoScriptResultado
        (
            ["arq1.sql", "arq2.sql"],
            [("antigo.sql", "novo.sql")]
        );

        var resumo = new ResumoScriptsMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("## Resumo dos pacotes gerados");
        output.ShouldContain("- `arq1.sql`");
        output.ShouldContain("- `arq2.sql`");
        output.ShouldContain("- `antigo.sql` » `novo.sql`");
        output.ShouldContain("---");
    }
}

/// <summary>
/// Testes para ResumoScriptsConsole.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumoScriptsConsoleTestes
{
    [Fact]
    public void ExibirRelatorio_DeveExibirResumoConsoleCorreto()
    {
        // Arrange
        var console = new TestConsole();
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.GetCurrentDirectory().Returns("");

        var resultado = new EmpacotamentoScriptResultado
        (
            ["arq1.sql", "arq2.sql"],
            [("antigo.sql", "novo.sql")]
        );

        var resumo = new ResumoScriptsConsole(console, fileSystem, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("Resumo dos pacotes gerados");
        output.ShouldContain("arq1.sql");
        output.ShouldContain("arq2.sql");
    }
}

/// <summary>
/// Testes para ResumoCmpkgMarkdown.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumoCmpkgMarkdownTestes
{
    [Fact]
    public void ExibirRelatorio_DeveExibirResumoMarkdownCorreto()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = new EmpacotamentoResultado("pacote.cmpkg", @"c:\saida\manifesto.dat", ["arq1.txt", "arq2.txt"]);
        var resumo = new ResumoCmpkgMarkdown(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("## Resumo do empacotamento");
        output.ShouldContain("- Pacote gerado: `pacote.cmpkg`");
        output.ShouldContain("- Manifesto.dat: `c:\\saida\\manifesto.dat`");
        output.ShouldContain("- `arq1.txt`");
        output.ShouldContain("- `arq2.txt`");
        output.ShouldContain("---");
    }
}

/// <summary>
/// Testes para ResumoCmpkgConsole.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumoCmpkgConsoleTestes
{
    [Fact]
    public void ExibirRelatorio_DeveExibirResumoConsoleCorreto()
    {
        // Arrange
        var console = new TestConsole();
        var resultado = new EmpacotamentoResultado("/tmp/pacote.cmpkg", @"c:\saida\manifesto.dat", ["/tmp/arq1.txt", "/tmp/arq2.txt"]);
        var resumo = new ResumoCmpkgConsole(console, resultado);

        // Act
        resumo.ExibirRelatorio();
        var output = console.Output;

        // Assert
        output.ShouldContain("Resumo do Empacotamento");
        output.ShouldContain("Pasta do pacote gerado:");
        output.ShouldContain("manifesto.dat");
        output.ShouldContain("arq1.txt");
        output.ShouldContain("arq2.txt");
    }
}
