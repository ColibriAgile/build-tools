using System.Diagnostics.CodeAnalysis;
using BuildTools.Services;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para o provedor de data e hora.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DateTimeProviderTestes
{
    private readonly DateTimeProvider _provider = new();

    [Fact]
    public void UtcNow_DeveRetornarDataAtual()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var resultado = _provider.UtcNow;

        // Assert
        var depois = DateTime.UtcNow;
        resultado.ShouldBeGreaterThanOrEqualTo(antes);
        resultado.ShouldBeLessThanOrEqualTo(depois);
    }

    [Fact]
    public void UtcNow_DeveRetornarHorarioUtc()
    {
        // Act
        var resultado = _provider.UtcNow;

        // Assert
        resultado.Kind.ShouldBe(DateTimeKind.Utc);
    }
}
