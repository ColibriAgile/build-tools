namespace BuildTools.Services;

/// <summary>
/// Serviço responsável por gerar tokens JWT.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Gera um token JWT com as configurações padrão do sistema.
    /// </summary>
    /// <returns>Token JWT como string.</returns>
    string GerarToken();
}
