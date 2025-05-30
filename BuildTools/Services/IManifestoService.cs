using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Serviço para manipulação de manifestos de empacotamento.
/// </summary>
public interface IManifestoService
{
    /// <summary>
    /// Lê o manifesto de uma pasta.
    /// </summary>
    /// <param name="pasta">Caminho da pasta.</param>
    /// <returns>Manifesto lido.</returns>
    Manifesto LerManifesto(string pasta);    
    
    /// <summary>
    /// Salva o manifesto na pasta informada.
    /// </summary>
    /// <param name="pasta">Caminho da pasta.</param>
    /// <param name="manifesto">Manifesto a ser salvo.</param>
    void SalvarManifesto(string pasta, Manifesto manifesto);

    /// <summary>
    /// Lê o manifesto de deploy (manifesto.dat) de uma pasta.
    /// </summary>
    /// <param name="pasta">Caminho da pasta contendo o arquivo manifesto.dat.</param>
    /// <returns>Manifesto de deploy lido.</returns>
    Task<ManifestoDeploy> LerManifestoDeployAsync(string pasta);
}
