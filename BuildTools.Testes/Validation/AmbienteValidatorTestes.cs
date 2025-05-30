using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using BuildTools.Validation;

namespace BuildTools.Testes.Validation;

/// <summary>
/// Testes para a classe <see cref="AmbienteValidator"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AmbienteValidatorTestes
{
    private const string AMBIENTE_DESENVOLVIMENTO = "desenvolvimento";
    private const string AMBIENTE_HOMOLOGACAO = "stage";
    private const string AMBIENTE_PRODUCAO = "producao";
    private const string AMBIENTE_INVALIDO = "ambiente-inexistente";

    [Theory]
    [InlineData(AMBIENTE_DESENVOLVIMENTO)]
    [InlineData(AMBIENTE_HOMOLOGACAO)]
    [InlineData(AMBIENTE_PRODUCAO)]
    public void ValidarAmbiente_AmbienteValido_DeveValidar(string ambiente)
    {
        // Act
        var validacao = () => AmbienteValidator.ValidarAmbiente(ambiente);

        // Assert
        validacao.ShouldNotThrow();
    }

    [Fact]
    public void ValidarAmbiente_AmbienteInvalido_DeveLancarArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(static () => AmbienteValidator.ValidarAmbiente(AMBIENTE_INVALIDO))
            .Message.ShouldContain($"Ambiente '{AMBIENTE_INVALIDO}' inválido");
    }

    [Fact]
    public void ValidarAmbiente_AmbienteNulo_DeveLancarArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(static () => AmbienteValidator.ValidarAmbiente(null!));
    }

    [Fact]
    public void ValidarAmbiente_AmbienteVazio_DeveLancarArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(static () => AmbienteValidator.ValidarAmbiente(string.Empty));
    }

    [Fact]
    public void ValidarAmbiente_AmbienteComCaseDiferente_DeveValidar()
    {
        // Arrange
        const string ambienteUpperCase = "DESENVOLVIMENTO";

        // Act
        var validacao = () => AmbienteValidator.ValidarAmbiente(ambienteUpperCase);

        // Assert
        validacao.ShouldNotThrow();
    }

    [Theory]
    [InlineData("  desenvolvimento  ")]
    [InlineData("\t\nstage\t\n")]
    public void ValidarAmbiente_AmbienteComEspacos_DeveLancarArgumentException(string ambienteComEspacos)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => AmbienteValidator.ValidarAmbiente(ambienteComEspacos))
            .Message.ShouldContain("inválido");
    }

    [Fact]
    public void CriarOpcaoAmbiente_DeveRetornarOpcaoConfigurada()
    {
        // Act
        var opcao = AmbienteValidator.CriarOpcaoAmbiente();

        // Assert
        opcao.ShouldNotBeNull();
        opcao.Name.ShouldBe("ambiente");
        opcao.Aliases.ShouldContain("--ambiente");
        opcao.Aliases.ShouldContain("-a");
        opcao.IsRequired.ShouldBeFalse();
    }

    [Fact]
    public void CriarOpcaoAmbiente_ComValorPadraoCustomizado_DeveConfigurarCorreto()
    {
        // Arrange
        const string VALOR_PADRAO_CUSTOMIZADO = "producao";

        // Act
        var opcao = AmbienteValidator.CriarOpcaoAmbiente(VALOR_PADRAO_CUSTOMIZADO);

        // Assert
        opcao.ShouldNotBeNull();
        opcao.Name.ShouldBe("ambiente");
        opcao.Aliases.ShouldContain("--ambiente");
        opcao.Aliases.ShouldContain("-a");
        opcao.IsRequired.ShouldBeFalse();
    }

    [Fact]
    public void CriarOpcaoAmbiente_SemParametros_DeveUsarDesenvolvimentoComoPadrao()
    {
        // Act
        var opcao = AmbienteValidator.CriarOpcaoAmbiente();

        // Assert
        opcao.ShouldNotBeNull();
        opcao.Name.ShouldBe("ambiente");
        opcao.IsRequired.ShouldBeFalse();
    }

    [Theory]
    [InlineData(AMBIENTE_DESENVOLVIMENTO)]
    [InlineData(AMBIENTE_HOMOLOGACAO)]
    [InlineData(AMBIENTE_PRODUCAO)]
    public void CriarOpcaoAmbiente_ValidarComandoValido_DeveAceitarAmbiente(string ambiente)
    {
        // Arrange
        var opcao = AmbienteValidator.CriarOpcaoAmbiente();
        var command = new Command("test");
        command.AddOption(opcao);

        // Act
        var parseResult = command.Parse($"--ambiente {ambiente}");

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValueForOption(opcao).ShouldBe(ambiente);
    }

    [Fact]
    public void CriarOpcaoAmbiente_ValidarComandoInvalido_DeveRejeitarAmbiente()
    {
        // Arrange
        var opcao = AmbienteValidator.CriarOpcaoAmbiente();
        var command = new Command("test");
        command.AddOption(opcao);

        // Act
        var parseResult = command.Parse($"--ambiente {AMBIENTE_INVALIDO}");

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
        parseResult.Errors[0].Message.ShouldContain($"Ambiente '{AMBIENTE_INVALIDO}' inválido");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("test")]
    public void CriarOpcaoAmbiente_ValidarComandoComAmbientesInvalidos_DeveRejeitarTodos(string ambienteInvalido)
    {
        // Arrange
        var opcao = AmbienteValidator.CriarOpcaoAmbiente();
        var command = new Command("test");
        command.AddOption(opcao);

        // Act
        var parseResult = command.Parse($"--ambiente {ambienteInvalido}");

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
        parseResult.Errors[0].Message.ShouldContain("inválido");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarOpcaoAmbiente_ValidarComandoComAmbientesVazio_DeveRejeitarTodos(string ambienteVazio)
    {
        // Arrange
        var opcao = AmbienteValidator.CriarOpcaoAmbiente();
        var command = new Command("test");
        command.AddOption(opcao);

        // Act
        var parseResult = command.Parse($"--ambiente {ambienteVazio}");

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
        parseResult.Errors[0].Message.ShouldContain("Required argument missing");
    }

    [Fact]
    public void ValidarAmbiente_TodosAmbientesValidos_DeveValidarTodos()
    {
        // Arrange
        var ambientesValidos = new[] { "desenvolvimento", "producao", "stage" };

        // Act & Assert
        foreach (var ambiente in ambientesValidos)
        {
            var validacao = () => AmbienteValidator.ValidarAmbiente(ambiente);
            validacao.ShouldNotThrow($"Ambiente '{ambiente}' deveria ser válido");
        }
    }

    [Fact]
    public void ValidarAmbiente_MensagemDeErro_DeveConterAmbientesValidos()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(static () => AmbienteValidator.ValidarAmbiente(AMBIENTE_INVALIDO));
        exception.Message.ShouldContain("desenvolvimento");
        exception.Message.ShouldContain("producao");
        exception.Message.ShouldContain("stage");
        exception.Message.ShouldContain("Valores permitidos:");
    }

    [Fact]
    public void ObterAmbientesValidos_DeveRetornarTodosAmbientesValidos()
    {
        // Act
        var ambientes = AmbienteValidator.ObterAmbientesValidos();

        // Assert
        ambientes.ShouldNotBeNull();
        ambientes.Length.ShouldBe(3);
        ambientes.ShouldContain("desenvolvimento");
        ambientes.ShouldContain("producao");
        ambientes.ShouldContain("stage");
    }
}
