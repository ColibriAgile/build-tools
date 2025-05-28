using System.Text;
using System.Text.Json;
using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço de notificação do marketplace.
/// </summary>
public sealed class MarketplaceService : IMarketplaceService
{
    private readonly HttpClient _httpClient;
    private readonly IJwtService _jwtService;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="MarketplaceService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP para comunicação com o marketplace.</param>
    /// <param name="jwtService">Serviço JWT para autenticação.</param>
    public MarketplaceService(HttpClient httpClient, IJwtService jwtService)
    {
        _httpClient = httpClient;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Notifica o marketplace sobre um novo pacote.
    /// </summary>
    /// <param name="urlMarketplace">URL base do marketplace.</param>
    /// <param name="manifesto">Dados do manifesto do pacote.</param>
    /// <returns>True se a notificação foi bem-sucedida, false caso contrário.</returns>
    public async Task<bool> NotificarPacoteAsync(string urlMarketplace, ManifestoDeploy manifesto)
    {
        try
        {
            var token = _jwtService.GerarToken();
            var url = $"{urlMarketplace.TrimEnd('/')}/api/notificar";

            var payload = new
            {
                nome = manifesto.Nome,
                versao = manifesto.Versao,
                manifesto = manifesto.DadosCompletos
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
