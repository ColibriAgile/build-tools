using Spectre.Console;
using System.CommandLine.IO;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace BuildTools.Services;

/// <summary>
/// Wrapper para o console ANSI do Spectre.Console, implementando a interface <see cref="System.CommandLine.IConsole"/>.
/// </summary>
public sealed class AnsiConsoleWrapper : IConsole
{
    private sealed class SpectreWriter(Action<string> write) : IStandardStreamWriter
    {
        public void Write(string? value)
        {
            if (value is not null)
                write(value);
        }
    }

    [ExcludeFromCodeCoverage]
    public AnsiConsoleWrapper()
        : this(AnsiConsole.Console) { }

    public AnsiConsoleWrapper(IAnsiConsole ansiConsole)
    {
        var ansiConsole1 = ansiConsole;
        Out = new SpectreWriter(ansiConsole1.Write);
        Error = new SpectreWriter(s => ansiConsole1.MarkupInterpolated($"[red][[ERROR]] {s}[/]"));
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public IStandardStreamWriter Out { get; set; }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public bool IsOutputRedirected { get; set; } = false;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public IStandardStreamWriter Error { get; set; }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public bool IsErrorRedirected { get; set; } = false;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public bool IsInputRedirected { get; set; } = false;
}
