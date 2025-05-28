using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Services;

/// <summary>
/// Implementação padrão do provedor de data e hora.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow
        => DateTime.UtcNow;
}
