using System.IO.Abstractions;
using Spectre.Console;

namespace BuildTools.Services;

/// <summary>
/// Service for listing files in a directory and displaying them using Spectre.Console.
/// </summary>
public sealed class FileListingService
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;

    public FileListingService(IFileSystem fileSystem, IAnsiConsole console)
    {
        _fileSystem = fileSystem;
        _console = console;
    }

    /// <summary>
    /// Lists files in the given directory and displays them using Spectre.Console.
    /// </summary>
    /// <param name="directory">Directory path</param>
    public string[] ListAndDisplayFiles(string directory)
    {
        var files = _fileSystem.Directory.GetFiles(directory);

        _console.MarkupLine("[blue]Arquivos encontrados:[/]");

        foreach (var file in files)
        {
            _console.MarkupLineInterpolated($" - {_fileSystem.Path.GetFileName(file)}");
        }

        return files;
    }
}
