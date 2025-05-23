using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Serviço responsável por gerar o manifesto expandido (manifesto.dat) a partir do manifesto original (com patterns e nomes).
/// </summary>
public interface IManifestoGeradorService
{
    /// <summary>
    /// Gera o manifesto.dat expandido, resolvendo patterns e nomes, e salva na pasta informada.
    /// </summary>
    /// <param name="pasta">Pasta de origem dos arquivos.</param>
    /// <param name="manifestoOriginal">Manifesto original (pode conter patterns).</param>
    /// <returns>Manifesto expandido (apenas nomes reais).</returns>
    Manifesto GerarManifestoExpandido(string pasta, Manifesto manifestoOriginal);
}
