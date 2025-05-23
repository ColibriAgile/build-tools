using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using BuildTools.Services;
using Spectre.Console.Testing;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para EmpacotadorScriptsService.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EmpacotadorScriptsServiceTestes
{
    private readonly TestConsole _console = new();
    private readonly IZipService _zipService = Substitute.For<IZipService>();

    private EmpacotadorScriptsService CriarService(IFileSystem fs)
    {
        _zipService.WhenForAnyArgs
        (
            static zip => zip.CompactarZip(Arg.Any<string>(), Arg.Any<List<(string Antigo, string Novo)>>(), Arg.Any<string>())
        ).Do
        (
            x =>
            {
                var destino = (string)x.Args()[2];
                fs.File.WriteAllText(destino, "");
            }
        );

        return new EmpacotadorScriptsService(fs, _console, _zipService);
    }

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
        var resultado = service.TemConfigJson(@"c:\pasta");

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
        Should.Throw<InvalidOperationException>(() => service.TemConfigJson(@"c:\pasta"));
    }

    [Fact]
    public void TemConfigJson_ArquivoNaoExiste_DeveRetornarFalse()
    {
        // Arrange
        var fs = new MockFileSystem();
        var service = CriarService(fs);

        // Act
        var resultado = service.TemConfigJson(@"c:\pasta");

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

        fs.AddDirectory(@"c:\pasta\sub");
        var service = CriarService(fs);

        // Act
        var arquivos = service.ListarArquivosComRelativo(@"c:\pasta").ToList();

        // Assert
        arquivos.ShouldContain(static x => x.CaminhoNoZip == "um.sql");
        arquivos.ShouldContain(static x => x.CaminhoNoZip == "dois.migration");
        arquivos.ShouldContain(static x => x.CaminhoNoZip == @"sub\abc.sql");
        arquivos.ShouldContain(static x => x.CaminhoNoZip == "config.json");
    }

    [Fact]
    public void Empacotar_QuandoPastaExiste_DeveExecutarComSucesso()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\um.sql", new MockFileData("select 1;") },
            { @"c:\pasta\dois.migration", new MockFileData("migra") },
            { @"c:\pasta\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\pasta\sub\abc.sql", new MockFileData("select 2;") }
        });

        var service = CriarService(fs);

        // Act
        var result = service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, false, false);

        // Assert
        result.ShouldNotBeNull();
        result.ArquivosGerados.ShouldHaveSingleItem();
        result.ArquivosGerados[0].ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts.zip"));
        _console.Output.ShouldContain("SUCCESS");
    }

    [Fact]
    public void Empacotar_QuandoZipDestinoExiste_DeveApagarZip()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida\";
        var zipFile = new MockFileData("");

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\um.sql", new MockFileData("select 1;") },
            { @"c:\pasta\dois.migration", new MockFileData("migra") },
            { @"c:\pasta\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\pasta\sub\abc.sql", new MockFileData("select 2;") },
            { @"c:\saida\_scripts.zip", zipFile }
        });

        var service = CriarService(fs);

        // Act
        service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, false, false);

        // Assert
        fs.GetFile(@"c:\saida\_scripts.zip").ShouldNotBe(zipFile);
    }

    [Fact]
    public void Empacotar_QuandoNaoHaArquivosNaPasta_DeveExibirAviso()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida\";

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\config.json", new MockFileData("{\"ok\":true}") },
        });

        var service = CriarService(fs);

        // Act
        service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, false, false);

        // Assert
        _console.Output.ShouldContain("Nenhum arquivo de script encontrado");
    }

    [Fact]
    public void Empacotar_QuandoPadronizarNomesFalse_NaoDeveChamarPadronizarNomes()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida\";

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\um.sql", new MockFileData("select 1;") },
            { @"c:\pasta\dois.migration", new MockFileData("migra") },
            { @"c:\pasta\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\pasta\sub\abc.sql", new MockFileData("select 2;") }
        });

        var service = CriarService(fs);

        // Act
        var resultado = service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, false, false);

        // Assert
        _console.Output.ShouldNotContain("renomeado");
        _zipService.Received().CompactarZip(PASTA_ORIGEM, Arg.Any<List<(string Antigo, string Novo)>>(), Path.Combine(PASTA_SAIDA, "_scripts.zip"));
        resultado.ArquivosRenomeados.ShouldBeEmpty();
    }

    [Fact]
    public void Empacotar_QuandoPadronizarNomes_DevePadronizarNomes()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida\";

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\um.sql", new MockFileData("select 1;") },
            { @"c:\pasta\dois.migration", new MockFileData("migra") },
            { @"c:\pasta\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\pasta\sub\abc.sql", new MockFileData("select 2;") }
        });

        var service = CriarService(fs);

        // Act
        var resultado = service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, false);

        // Assert
        _console.Output.ShouldContain("renomeado");
        resultado.ArquivosRenomeados.ShouldHaveSingleItem();
        resultado.ArquivosRenomeados[0].Antigo.ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts.zip"));
        resultado.ArquivosRenomeados[0].Novo.ShouldBe(Path.Combine(PASTA_SAIDA, "scripts.zip"));
    }

    [Fact]
    public void Empacotar_ComPadronizaNomesESubpastas_PadronizaNomes()
    {
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        const string PASTA_1 = "01_dconnect_pgsql";
        const string PASTA_2 = "02_dconnect_mssql";
        const string PASTA_3 = "05_master";
        const string PASTA_4 = "uteis";

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $@"{PASTA_ORIGEM}\{PASTA_1}\um.sql", new MockFileData("select 1;") },
            { $@"{PASTA_ORIGEM}\{PASTA_1}\dois.migration", new MockFileData("migra") },
            { $@"{PASTA_ORIGEM}\{PASTA_1}\config.json", new MockFileData("{\"ok\":true}") },
            { $@"{PASTA_ORIGEM}\{PASTA_1}\sub\abc.sql", new MockFileData("select 2;") },
            { $@"{PASTA_ORIGEM}\{PASTA_2}\um.sql", new MockFileData("select 1;") },
            { $@"{PASTA_ORIGEM}\{PASTA_2}\dois.migration", new MockFileData("migra") },
            { $@"{PASTA_ORIGEM}\{PASTA_2}\config.json", new MockFileData("{\"ok\":true}") },
            { $@"{PASTA_ORIGEM}\{PASTA_2}\sub\abc.sql", new MockFileData("select 2;") },
            { $@"{PASTA_ORIGEM}\{PASTA_3}\um.sql", new MockFileData("select 1;") },
            { $@"{PASTA_ORIGEM}\{PASTA_3}\dois.migration", new MockFileData("migra") },
            { $@"{PASTA_ORIGEM}\{PASTA_3}\config.json", new MockFileData("{\"ok\":true}") },
            { $@"{PASTA_ORIGEM}\{PASTA_3}\sub\abc.sql", new MockFileData("select 2;") },
            { $@"{PASTA_ORIGEM}\{PASTA_4}\um.sql", new MockFileData("select 1;") },
            { $@"{PASTA_ORIGEM}\{PASTA_4}\dois.migration", new MockFileData("migra") },
            { $@"{PASTA_ORIGEM}\{PASTA_4}\sub\abc.sql", new MockFileData("select 2;") }
        });

        var service = CriarService(fs);

        // Act
        var resultado = service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, false);
        resultado.ArquivosGerados.Count.ShouldBe(3);
        resultado.ArquivosGerados[0].ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts01_dconnect_pgsql.zip"));
        resultado.ArquivosGerados[1].ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts02_dconnect_mssql.zip"));
        resultado.ArquivosGerados[2].ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts05_master.zip"));
        resultado.ArquivosRenomeados.Count.ShouldBe(3);
        resultado.ArquivosRenomeados[0].Antigo.ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts01_dconnect_pgsql.zip"));
        resultado.ArquivosRenomeados[0].Novo.ShouldBe(Path.Combine(PASTA_SAIDA, "scripts_dconnect_pgsql.zip"));
        resultado.ArquivosRenomeados[1].Antigo.ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts02_dconnect_mssql.zip"));
        resultado.ArquivosRenomeados[1].Novo.ShouldBe(Path.Combine(PASTA_SAIDA, "scripts_dconnect_mssql.zip"));
        resultado.ArquivosRenomeados[2].Antigo.ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts05_master.zip"));
        resultado.ArquivosRenomeados[2].Novo.ShouldBe(Path.Combine(PASTA_SAIDA, "scripts_master.zip"));
    }

    [Fact]
    public void Empacotar_QuandoSubpastasValidas_DeveEmpacotarCadaSubpasta()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida\";

        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\pasta\01abc\um.sql", new MockFileData("select 1;") },
            { @"c:\pasta\01abc\dois.migration", new MockFileData("migra") },
            { @"c:\pasta\01abc\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\pasta\01abc\sub\abc.sql", new MockFileData("select 2;") },
            { @"c:\pasta\02\um.sql", new MockFileData("select 1;") },
            { @"c:\pasta\02\dois.migration", new MockFileData("migra") },
            { @"c:\pasta\02\config.json", new MockFileData("{\"ok\":true}") },
            { @"c:\pasta\02\sub\abc.sql", new MockFileData("select 2;") }
        });

        var service = CriarService(fs);

        // Act
        var resultado = service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, false, false);

        // Assert
        _zipService.Received(2).CompactarZip(Arg.Any<string>(), Arg.Any<List<(string Antigo, string Novo)>>(), Arg.Any<string>());
        resultado.ArquivosGerados.Count.ShouldBe(2);
        resultado.ArquivosGerados[0].ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts01abc.zip"));
        resultado.ArquivosGerados[1].ShouldBe(Path.Combine(PASTA_SAIDA, "_scripts02.zip"));
    }

    [Fact]
    public void Empacotar_QuandoCriarPastaSaidaFalha_DeveExibirErro()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida\";

        var fs = Substitute.For<IFileSystem>();
        fs.Directory.Exists(PASTA_ORIGEM).Returns(true);
        fs.Directory.Exists(PASTA_SAIDA).Returns(false);
        fs.Directory.CreateDirectory(PASTA_SAIDA).Returns(static _ => throw new UnauthorizedAccessException("Sem permissão"));

        var service = CriarService(fs);

        // Act & Assert
        Should.Throw<UnauthorizedAccessException>(() => service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, false, false));
        _console.Output.ShouldContain("Sem permissão");
    }

    [Fact]
    public void Empacotar_QuandoPadronizarNomesException_ImprimeErro()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida";
        var fs = Substitute.For<IFileSystem>();
        fs.Directory.Exists(PASTA_ORIGEM).Returns(true);
        fs.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        var arquivos = new List<string> { "arq1.sql", "arq2.sql" };
        fs.Directory.EnumerateFiles(PASTA_ORIGEM, "*.sql", SearchOption.AllDirectories).Returns(arquivos);
        fs.File.Exists(Path.Combine(PASTA_ORIGEM, "config.json")).Returns(true);
        fs.File.WhenForAnyArgs(static f => f.Move("", "", overwrite: true)).Throws(new IOException("Erro ao mover arquivo"));
        fs.File.ReadAllText(Arg.Any<string>()).Returns(""""{"chave": 1}"""");
        var service = CriarService(fs);

        // Act
        Should.Throw<IOException>(() => service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, true, false));

        // Assert
        _console.Output.ShouldContain("Erro ao mover arquivo");
    }

    [Fact]
    public void Empacotar_PastaOrigemNaoExiste_ImprimeErro()
    {
        // Arrange
        const string PASTA_ORIGEM = @"C:\pasta";
        const string PASTA_SAIDA = @"C:\saida\";
        var fs = Substitute.For<IFileSystem>();
        fs.Directory.Exists(PASTA_ORIGEM).Returns(false);
        fs.Directory.CreateDirectory(PASTA_SAIDA).Returns(Substitute.For<IDirectoryInfo>());
        var service = CriarService(fs);

        // Act
        Should.Throw<DirectoryNotFoundException>(() => service.Empacotar(PASTA_ORIGEM, PASTA_SAIDA, false, false));

        // Assert
        _console.Output.ShouldContain("A pasta de origem não existe");
    }
}