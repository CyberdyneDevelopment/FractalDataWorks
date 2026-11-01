using System;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Represents a symbol-level change captured for the migration guide.
/// </summary>
public sealed class SymbolChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolChange"/> class.
    /// </summary>
    /// <param name="oldFullyQualifiedName">The fully-qualified name before the change.</param>
    /// <param name="newFullyQualifiedName">The fully-qualified name after the change.</param>
    /// <param name="changeType">The change type name (a <see cref="SymbolChangeTypes"/> option name).</param>
    /// <param name="symbolKind">The symbol kind.</param>
    /// <param name="oldFilePath">The file path before the change, if applicable.</param>
    /// <param name="newFilePath">The file path after the change, if applicable.</param>
    /// <param name="oldAssembly">The assembly that contained the symbol before the change, if applicable.</param>
    /// <param name="newAssembly">The assembly that contains the symbol after the change, if applicable.</param>
    /// <param name="relativePosition">The symbol's position relative to its owning project root, if applicable.</param>
    public SymbolChange(
        string oldFullyQualifiedName,
        string newFullyQualifiedName,
        string changeType,
        string symbolKind,
        string? oldFilePath,
        string? newFilePath,
        string? oldAssembly,
        string? newAssembly,
        string? relativePosition)
    {
        OldFullyQualifiedName = oldFullyQualifiedName ?? throw new ArgumentNullException(nameof(oldFullyQualifiedName));
        NewFullyQualifiedName = newFullyQualifiedName ?? throw new ArgumentNullException(nameof(newFullyQualifiedName));
        ChangeType = changeType ?? throw new ArgumentNullException(nameof(changeType));
        SymbolKind = symbolKind ?? throw new ArgumentNullException(nameof(symbolKind));
        OldFilePath = oldFilePath;
        NewFilePath = newFilePath;
        OldAssembly = oldAssembly;
        NewAssembly = newAssembly;
        RelativePosition = relativePosition;
    }

    /// <summary>
    /// Gets the fully-qualified name before the change.
    /// </summary>
    public string OldFullyQualifiedName { get; }

    /// <summary>
    /// Gets the fully-qualified name after the change.
    /// </summary>
    public string NewFullyQualifiedName { get; }

    /// <summary>
    /// Gets the change type name (a <see cref="SymbolChangeTypes"/> option name).
    /// </summary>
    public string ChangeType { get; }

    /// <summary>
    /// Gets the symbol kind (e.g. "NamedType", "Method", "Property").
    /// </summary>
    public string SymbolKind { get; }

    /// <summary>
    /// Gets the file path before the change, if applicable.
    /// </summary>
    public string? OldFilePath { get; }

    /// <summary>
    /// Gets the file path after the change, if applicable.
    /// </summary>
    public string? NewFilePath { get; }

    /// <summary>
    /// Gets the assembly that contained the symbol before the change, or <see langword="null"/> when the
    /// change did not cross an assembly boundary.
    /// </summary>
    public string? OldAssembly { get; }

    /// <summary>
    /// Gets the assembly that contains the symbol after the change, or <see langword="null"/> when the
    /// change did not cross an assembly boundary.
    /// </summary>
    public string? NewAssembly { get; }

    /// <summary>
    /// Gets the symbol's path relative to its owning project root (its position within the service tree),
    /// or <see langword="null"/> when not recorded.
    /// </summary>
    public string? RelativePosition { get; }

    /// <summary>
    /// Gets a value indicating whether this change moved the symbol between assemblies.
    /// </summary>
    public bool CrossesAssembly =>
        OldAssembly is not null && NewAssembly is not null &&
        !string.Equals(OldAssembly, NewAssembly, StringComparison.Ordinal);
}
