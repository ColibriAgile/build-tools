using System.IO.Abstractions;
using Spectre.Console;

namespace BuildTools.Services;

/// <summary>
/// Service for listing files in a directory and displaying them using Spectre.Console.
/// </summary>
public sealed class FileListingService
{
    private readonly IFileSystem _fileSystem;

    public FileListingService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Lists files in the given directory and displays them using Spectre.Console.
    /// </summary>
    /// <param name="directory">Directory path</param>
    public string[] ListAndDisplayFiles(string directory)
    {
        var files = _fileSystem.Directory.GetFiles(directory);

        AnsiConsole.MarkupLine("[blue]Arquivos encontrados:[/]");

        foreach (var file in files)
        {
            AnsiConsole.MarkupLine($" - {_fileSystem.Path.GetFileName(file)}");
        }

        return files;
    }
}
