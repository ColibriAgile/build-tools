using BuildTools.Services;
using Spectre.Console;
using Spectre.Console.Testing;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para o wrapper AnsiConsoleWrapper.
/// </summary>
public sealed class AnsiConsoleWrapperTestes
{
    [Fact]
    public void Out_DeveEscreverNoConsole()
    {
        // Arrange
        var testConsole = new TestConsole();
        var wrapper = new AnsiConsoleWrapper(testConsole);

        // Act
        wrapper.Out.Write("abc");

        // Assert
        testConsole.Output.ShouldContain("abc");
    }

    [Fact]
    public void Error_DeveEscreverNoConsoleComErro()
    {
        // Arrange
        var testConsole = new TestConsole();
        var wrapper = new AnsiConsoleWrapper(testConsole);

        // Act
        wrapper.Error.Write("erro123");

        // Assert
        testConsole.Output.ShouldContain("erro123");
        testConsole.Output.ShouldContain("[ERROR]");
    }

    [Fact]
    public void PropriedadesPadrao_NaoSaoRedirecionadas()
    {
        // Arrange
        var wrapper = new AnsiConsoleWrapper(Substitute.For<IAnsiConsole>());

        // Assert
        Assert.False(wrapper.IsOutputRedirected);
        Assert.False(wrapper.IsErrorRedirected);
        Assert.False(wrapper.IsInputRedirected);
    }
}
