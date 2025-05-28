namespace BuildTools.Services;

/// <summary>
/// Provedor de data e hora para permitir testes determinísticos.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Obtém a data e hora atual em UTC.
    /// </summary>
    /// <returns>Data e hora atual em UTC.</returns>
    DateTime UtcNow { get; }
}
