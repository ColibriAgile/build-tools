// Arquivo: d:\projetos\BuildTools\BuildTools\Services\VersaoBaseService.cs
using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço para atualização das versões de bases compatíveis no manifesto.
/// </summary>
/// <inheritdoc cref="IVersaoBaseService"/>
public sealed class VersaoBaseService : IVersaoBaseService
{
    /// <inheritdoc />
    public void AtualizarVersoesBases(Manifesto manifesto, IEnumerable<(string schema, string versao)> versoesBases)
    {
        if (versoesBases is null)
            return;

        var lista = versoesBases
            .Select(v => new VersaoBase { Schema = v.schema, Versao = v.versao })
            .ToList();
            
        if (lista.Count > 0)
            manifesto.VersoesBases = lista;
    }
}
