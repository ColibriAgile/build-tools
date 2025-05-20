// Arquivo: d:\projetos\BuildTools\BuildTools\Services\IVersaoBaseService.cs
using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Serviço para atualização das versões de bases compatíveis no manifesto.
/// </summary>
public interface IVersaoBaseService
{
    /// <summary>
    /// Atualiza as versões de bases compatíveis no manifesto.
    /// </summary>
    /// <param name="manifesto">Manifesto a ser atualizado.</param>
    /// <param name="versoesBases">Lista de versões de base (schema, versao).</param>
    void AtualizarVersoesBases(Manifesto manifesto, IEnumerable<(string schema, string versao)> versoesBases);
}
