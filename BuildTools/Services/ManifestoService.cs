using BuildTools.Constants;
using BuildTools.Models;
using System.Text.Json;
using System.IO.Abstractions;
using System.Text.Json.Serialization;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço de manipulação de manifestos de empacotamento.
/// </summary>
/// <param name="fileSystem">
/// Abstração do sistema de arquivos.
/// </param>
/// <inheritdoc cref="IManifestoService"/>
public sealed class ManifestoService(IFileSystem fileSystem) : IManifestoService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Nome do arquivo de manifesto server.
    /// </summary>
    private const string MANIFESTO_SERVER = "manifesto.server";

    /// <summary>
    /// Nome do arquivo de manifesto local.
    /// </summary>
    private const string MANIFESTO_LOCAL = "manifesto.local";

    /// <inheritdoc />
    public Manifesto LerManifesto(string pasta)
    {
        var caminhos = new[]
        {
            Path.Combine(pasta, MANIFESTO_SERVER),
            Path.Combine(pasta, MANIFESTO_LOCAL)
        };

        foreach (var caminho in caminhos)
        {
            if (!fileSystem.File.Exists(caminho))
                continue;

            var json = fileSystem.File.ReadAllText(caminho);

            return JsonSerializer.Deserialize<Manifesto>(json) ?? throw new InvalidOperationException($"Manifesto inválido: {caminho}");
        }

        throw new FileNotFoundException("Nenhum manifesto encontrado na pasta.", string.Join(", ", caminhos));
    }

    /// <inheritdoc />
    public void SalvarManifesto(string pasta, Manifesto manifesto)
    {
        var caminho = Path.Combine(pasta, EmpacotadorConstantes.MANIFESTO);

        var json = JsonSerializer.Serialize(manifesto, _jsonSerializerOptions);
        fileSystem.Directory.CreateDirectory(pasta);
        fileSystem.File.WriteAllText(caminho, json);
    }
}
