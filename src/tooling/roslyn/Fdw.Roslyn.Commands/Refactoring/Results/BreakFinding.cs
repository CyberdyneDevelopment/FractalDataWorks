using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// Something that will break as a result of moving a type between assemblies.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BreakFinding
{
    /// <summary>Gets or sets the break kind.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the file the break was found in.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable description of the break.</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity label.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the moved type this finding belongs to, or empty when it cannot be attributed.
    /// </summary>
    /// <remarks>
    /// A caller reviewing a move of many types needs to know WHICH type each problem belongs to, not a
    /// flat list. Attribution is by the file the diagnostic sits in, then by the type named in its
    /// message; anything that matches neither is reported unattributed rather than guessed at.
    /// </remarks>
    public string AffectedType { get; set; } = string.Empty;
}
