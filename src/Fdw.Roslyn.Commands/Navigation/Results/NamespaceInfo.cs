namespace Fdw.Roslyn.Commands.Navigation.Results;

/// <summary>
/// Represents information about a namespace.
/// </summary>
public sealed class NamespaceInfo
{
    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a file-scoped namespace.
    /// </summary>
    public bool IsFileScopedNamespace { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public required int Column { get; init; }
}
