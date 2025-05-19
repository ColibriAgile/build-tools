using System.Text.Json;
using System.IO.Abstractions;

namespace BuildTools.Services;

/// <summary>
/// Service for reading the packaging manifest.
/// </summary>
public sealed class ManifestoService
{
    private readonly IFileSystem _fileSystem;
    private readonly IZipService _zipService;

    public ManifestoService(
        IFileSystem fileSystem,
        IZipService zipService
    )
    {
        _fileSystem = fileSystem;
        _zipService = zipService;
    }

    /// <summary>
    /// Reads the manifest file from the given folder, or returns a default manifest if not found.
    /// </summary>
    /// <param name="pasta">Folder path</param>
    /// <param name="manifestoUsado">Out: path of the manifest used, or empty if none found</param>
    /// <returns>Manifest as a dictionary</returns>
    public Dictionary<string, object> LerManifesto
    (
        string pasta,
        out string manifestoUsado
    )
    {
        var manifestoServer = _fileSystem.Path.Combine(pasta, "manifesto.server");
        var manifestoLocal = _fileSystem.Path.Combine(pasta, "manifesto.local");
        var manifestoDat = _fileSystem.Path.Combine(pasta, "manifesto.dat");
        manifestoUsado = string.Empty;
        var manifesto = new Dictionary<string, object>();

        if (_fileSystem.File.Exists(manifestoServer))
        {
            manifestoUsado = manifestoServer;
        }
        else if (_fileSystem.File.Exists(manifestoLocal))
        {
            manifestoUsado = manifestoLocal;
        }
        else if (_fileSystem.File.Exists(manifestoDat))
        {
            manifestoUsado = manifestoDat;
        }
        else
        {
            manifesto["versao"] = "1.0.0.0";
            return manifesto;
        }

        var json = _fileSystem.File.ReadAllText(manifestoUsado);
        var temp = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        if (temp != null)
        {
            manifesto = temp;
        }

        return manifesto;
    }

    /// <summary>
    /// Saves the manifest as a JSON file in the specified folder.
    /// </summary>
    /// <param name="pasta">Folder path</param>
    /// <param name="manifesto">Manifest data</param>
    public void SalvarManifesto(string pasta, Dictionary<string, object> manifesto)
    {
        var manifestoPath = _fileSystem.Path.Combine(pasta, "manifesto.dat");
        var json = JsonSerializer.Serialize(manifesto, new JsonSerializerOptions { WriteIndented = true });
        _fileSystem.File.WriteAllText(manifestoPath, json);
    }

    /// <summary>
    /// Updates the compatible database versions in the manifest.
    /// </summary>
    /// <param name="manifesto">Manifest data</param>
    /// <param name="versoesBases">List of (schema, version) tuples</param>
    public void AtualizarVersoesBases
    (
        Dictionary<string, object> manifesto,
        List<(string schema, string versao)> versoesBases
    )
    {
        const string BASES_COMPATIVEIS = "versoes_bases";
        const string SCHEMA = "schema";
        const string VERSAO = "versao";

        var novasBases = versoesBases
            .Select(a => new Dictionary<string, object> { [SCHEMA] = a.schema, [VERSAO] = a.versao })
            .ToList();

        var existentes = manifesto.ContainsKey(BASES_COMPATIVEIS)
            ? manifesto[BASES_COMPATIVEIS] as List<Dictionary<string, object>>
            : new List<Dictionary<string, object>>();

        if (existentes != null)
        {
            foreach (var baseExistente in existentes)
            {
                var schema = baseExistente.ContainsKey(SCHEMA) ? baseExistente[SCHEMA]?.ToString() : null;
                
                if (!string.IsNullOrEmpty(schema) && !novasBases.Any(b => b[SCHEMA]?.ToString() == schema))
                {
                    novasBases.Add(baseExistente);
                }
            }
        }

        if (novasBases.Count > 0)
        {
            manifesto[BASES_COMPATIVEIS] = novasBases;
        }
    }

    /// <summary>
    /// Lógica principal de empacotamento: atualiza manifesto, gera nome do pacote, exclui pacotes antigos, prepara para compactação e QA.
    /// </summary>
    /// <param name="pasta">Pasta de origem</param>
    /// <param name="pastaSaida">Pasta de saída</param>
    /// <param name="senha">Senha do pacote zip (opcional)</param>
    /// <returns>Caminho do pacote gerado</returns>
    public string Empacotar
    (
        string pasta,
        string pastaSaida,
        string senha = ""
    )
    {
        // Constantes
        const string MANIFESTO = "manifesto.dat";
        const string PACOTE = "nome";
        const string VERSAO = "versao";
        const string EXTENSAO_PACOTE = ".cmpkg";
        const string ARQUIVOS = "arquivos";

        // Lê ou cria o manifesto
        var manifesto = LerManifesto(pasta, out _);

        // TODO: Atualizar manifesto com argumentos da linha de comando, se necessário
        // TODO: Atualizar versões das bases, se necessário

        // Lista arquivos do diretório
        var arquivosNoDiretorio = _fileSystem.Directory.GetFiles(pasta)
            .Select(f => _fileSystem.Path.GetFileName(f))
            .Where(f => !string.IsNullOrEmpty(f) && f != MANIFESTO)
            .ToList();

        // Atualiza lista de arquivos no manifesto
        manifesto[ARQUIVOS] = arquivosNoDiretorio;

        // Salva manifesto atualizado
        SalvarManifesto(pasta, manifesto);

        // Gera prefixo e nome do pacote
        var nomePacote = manifesto.ContainsKey(PACOTE) && manifesto[PACOTE] != null
            ? manifesto[PACOTE]!.ToString() ?? "pacote"
            : "pacote";

        var prefixo = nomePacote.Replace(" ", string.Empty) + "_";

        var versaoPacote = manifesto.ContainsKey(VERSAO) && manifesto[VERSAO] != null
            ? manifesto[VERSAO]!.ToString() ?? "1.0.0.0"
            : "1.0.0.0";

        var versao = versaoPacote.Replace(" ", string.Empty).Replace(".", "_");
        var nomeCmpkg = prefixo + versao + EXTENSAO_PACOTE;
        var caminhoSaida = _fileSystem.Path.Combine(pastaSaida, nomeCmpkg);

        // Exclui pacotes antigos com o mesmo prefixo
        ExcluirComPrefixo(pastaSaida, prefixo, EXTENSAO_PACOTE);

        // Compacta arquivos em ZIP usando serviço injetado
        _zipService.CompactarZip
        (
            pasta,
            arquivosNoDiretorio,
            caminhoSaida,
            senha
        );

        // TODO: Copiar para pasta QA se necessário

        return caminhoSaida;
    }

    /// <summary>
    /// Exclui arquivos com determinado prefixo e extensão em uma pasta.
    /// </summary>
    /// <param name="pasta">Pasta de busca</param>
    /// <param name="prefixo">Prefixo do arquivo</param>
    /// <param name="extensao">Extensão do arquivo</param>
    private void ExcluirComPrefixo
    (
        string pasta,
        string prefixo,
        string extensao
    )
    {
        if (!_fileSystem.Directory.Exists(pasta))
        {
            return;
        }

        var arquivos = _fileSystem.Directory.GetFiles(pasta)
            .Where(f => _fileSystem.Path.GetFileName(f).StartsWith(prefixo) && f.EndsWith(extensao))
            .ToList();

        foreach (var arq in arquivos)
        {
            try
            {
                _fileSystem.File.Delete(arq);
            }
            catch
            {
                // Log/ignorar erro
            }
        }
    }
}
