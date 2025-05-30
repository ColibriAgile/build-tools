using System.Text;
using System.Text.Json;
using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço de notificação do marketplace.
/// </summary>
/// <remarks>
/// Inicializa uma nova instância da classe <see cref="MarketplaceService"/>.
/// </remarks>
/// <param name="httpClient">Cliente HTTP para comunicação com o marketplace.</param>
/// <param name="jwtService">Serviço JWT para autenticação.</param>
public sealed class MarketplaceService(HttpClient httpClient, IJwtService jwtService, IAnsiConsole console) : IMarketplaceService
{
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
            var token = jwtService.GerarToken();
            var url = $"{urlMarketplace.TrimEnd('/')}/api/secure/pacote/sync/";

            var payload = new
            {
                nome = manifesto.Nome,
                versao = manifesto.Versao,
                manifesto = manifesto.DadosCompletos
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("TOKEN", token);

            var response = await httpClient.PostAsync(url, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                console.MarkupLineInterpolated($"[red][[ERROR]]Erro ao notificar market: {(int)response.StatusCode} - {response.ReasonPhrase}[/]");
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                console.MarkupLineInterpolated($"[red]{responseContent}[/]");

                return false;
            }

            console.MarkupLine("[green][[SUCCESS]]Market Place notificado com sucesso.[/]");

            return true;
        }
        catch (Exception ex)
        {
            console.MarkupLineInterpolated($"[red][[ERROR]]Erro ao notificar market: {ex.Message}[/]");

            return false;
        }
    }

    /// <inheritdoc />
    public string ObterUrlMarketplace(string ambiente, string? urlMarketplace = null)
    {
        if (!string.IsNullOrEmpty(urlMarketplace))
            return urlMarketplace;

        var isTest = string.Equals(Environment.GetEnvironmentVariable("TEST"), "true", StringComparison.OrdinalIgnoreCase);

        if (isTest)
            return "http://localhost:8888";

        return ambiente.ToLowerInvariant() switch
        {
            "stage" => "https://qa-marketplace.ncrcolibri.com.br",
            "producao" => "https://marketplace.ncrcolibri.com.br",
            "desenvolvimento" => "https://qa-marketplace.ncrcolibri.com.br",
            var _ => throw new ArgumentException($"Ambiente '{ambiente}' não é válido. Valores permitidos: 'desenvolvimento', 'producao', 'stage'.", nameof(ambiente))
        };
    }
}
