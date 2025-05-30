namespace BuildTools.Services;

using BuildTools.Models;

/// <summary>
/// Serviço responsável por notificar o marketplace sobre novos pacotes.
/// </summary>
public interface IMarketplaceService
{
    /// <summary>
    /// Notifica o marketplace sobre um novo pacote.
    /// </summary>
    /// <param name="urlMarketplace">URL base do marketplace.</param>
    /// <param name="manifesto">Dados do manifesto do pacote.</param>
    /// <returns>True se a notificação foi bem-sucedida, false caso contrário.</returns>
    Task<bool> NotificarPacoteAsync(string urlMarketplace, ManifestoDeploy manifesto);

    /// <summary>
    /// Obtém a URL do marketplace baseada no ambiente ou na URL fornecida.
    /// </summary>
    /// <param name="ambiente">Ambiente de deploy.</param>
    /// <param name="urlMarketplace">URL customizada do marketplace.</param>
    /// <returns>URL final do marketplace.</returns>
    string ObterUrlMarketplace(string ambiente, string? urlMarketplace = null);
}
