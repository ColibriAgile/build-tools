using BuildTools.Constants;
using BuildTools.Models;
using System.Text.Json;
using System.IO.Abstractions;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço de manipulação de manifestos de empacotamento.
/// </summary>
/// <inheritdoc cref="IManifestoService"/>
public sealed class ManifestoService : IManifestoService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Nome do arquivo de manifesto server.
    /// </summary>
    private const string MANIFESTO_SERVER = "manifesto.server";

    /// <summary>
    /// Nome do arquivo de manifesto local.
    /// </summary>
    private const string MANIFESTO_LOCAL = "manifesto.local";

    private readonly IFileSystem _fileSystem;

    public ManifestoService(IFileSystem fileSystem)
        => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Manifesto LerManifesto(string pasta)
    {
        var caminhos = new[]
        {
            _fileSystem.Path.Combine(pasta, MANIFESTO_SERVER),
            _fileSystem.Path.Combine(pasta, MANIFESTO_LOCAL)
        };

        foreach (var caminho in caminhos)
        {
            if (!_fileSystem.File.Exists(caminho))
                continue;

            var json = _fileSystem.File.ReadAllText(caminho);

            return JsonSerializer.Deserialize<Manifesto>(json) ?? throw new InvalidOperationException($"Manifesto inválido: {caminho}");
        }

        throw new FileNotFoundException("Nenhum manifesto encontrado na pasta.", string.Join(", ", caminhos));
    }

    /// <inheritdoc />
    public void SalvarManifesto(string pasta, Manifesto manifesto)
    {
        var caminho = _fileSystem.Path.Combine(pasta, EmpacotadorConstantes.MANIFESTO);

        var json = JsonSerializer.Serialize(manifesto, _jsonSerializerOptions);
        _fileSystem.File.WriteAllText(caminho, json);
    }
}
