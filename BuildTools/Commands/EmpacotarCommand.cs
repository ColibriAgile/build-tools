using System.CommandLine;
using System.IO.Abstractions;
using System.Text.Json;
using BuildTools.Services;
using Spectre.Console;

namespace BuildTools.Commands;

/// <summary>
/// Command to package files from a folder according to the manifest.
/// </summary>
public sealed class EmpacotarCommand : Command
{
    private readonly IFileSystem _fileSystem;
    private readonly FileListingService _fileListingService;
    private readonly ManifestoService _manifestoService;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="EmpacotarCommand"/>.
    /// </summary>
    /// <param name="fileSystem">
    /// A abstração do sistema de arquivos a ser utilizada.
    /// </param>
    /// <param name="fileListingService">
    /// Serviço para listar arquivos em um diretório.
    /// </param>
    /// <param name="manifestoService">
    /// Serviço para ler o manifesto de empacotamento.
    /// </param>
    /// <param name="console">
    /// O console a ser usado para saída.
    /// </param>
    public EmpacotarCommand
    (
        IFileSystem fileSystem,
        FileListingService fileListingService,
        ManifestoService manifestoService,
        IAnsiConsole console
    ) : base("empacotar", "Empacota arquivos de uma pasta conforme manifesto.")
    {
        var pastaOption = new Option<string>
        (
            aliases: ["--pasta", "-p", "/pasta"],
            description: "Pasta de origem dos arquivos"
        )
        {
            IsRequired = true
        };

        var saidaOption = new Option<string>
        (
            aliases: ["--saida", "-s", "/saida"],
            description: "Pasta de saída"
        )
        {
            IsRequired = true
        };

        var senhaOption = new Option<string>
        (
            aliases: ["--senha", "-se", "/senha"],
            description: "Senha do pacote zip (opcional)"
        )
        {
            IsRequired = false
        };

        AddOption(pastaOption);
        AddOption(saidaOption);
        AddOption(senhaOption);

        _fileSystem = fileSystem;
        _fileListingService = fileListingService;
        _manifestoService = manifestoService;
        _console = console;

        this.SetHandler
        (
            Handle,
            pastaOption,
            saidaOption,
            senhaOption
        );
    }

    private void Handle(string pasta, string saida, string senha)
    {
        try
        {
            if (!_fileSystem.Directory.Exists(pasta))
            {
                _console.MarkupLine($"[red]A pasta de origem não existe:[/] {pasta}");

                return;
            }

            if (!_fileSystem.Directory.Exists(saida))
            {
                _fileSystem.Directory.CreateDirectory(saida);
                _console.MarkupLine($"[yellow]Pasta de saída criada:[/] {saida}");
            }

            var arquivos = _fileListingService.ListAndDisplayFiles(pasta);
            var manifesto = _manifestoService.LerManifesto(pasta, out string manifestoUsado);

            if (!string.IsNullOrEmpty(manifestoUsado))
            {
                _console.MarkupLine($"[yellow]Usando manifesto:[/] {manifestoUsado}");
            }
            else
            {
                _console.MarkupLine("[red]Nenhum manifesto encontrado. Será gerado um novo manifesto padrão.");
            }

            _console.MarkupLine("[green]Manifesto carregado:[/]");

            _console.WriteLine(JsonSerializer.Serialize
            (
                manifesto,
                new JsonSerializerOptions { WriteIndented = true }
            ));
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Erro:[/] {ex.Message}");
        }
    }   
}
