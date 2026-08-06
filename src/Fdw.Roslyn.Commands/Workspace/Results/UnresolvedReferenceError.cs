using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// A reference error the ledger could NOT explain.
/// </summary>
/// <remarks>
/// Reported rather than guessed at. An error the ledger does not cover was not caused by a recorded move,
/// so inventing a reference for it would paper over a different defect.
/// </remarks>
// Why: pure data holder, auto-properties only, no logic
[ExcludeFromCodeCoverage]
public sealed class UnresolvedReferenceError
{
    /// <summary>Gets or sets the project that failed to compile.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Gets or sets the diagnostic id.</summary>
    public string DiagnosticId { get; set; } = string.Empty;

    /// <summary>Gets or sets the file the error occurred in.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the 1-based line the error occurred on.</summary>
    public int Line { get; set; }

    /// <summary>Gets or sets the type or namespace the compiler could not find.</summary>
    public string MissingName { get; set; } = string.Empty;

    /// <summary>Gets or sets why the ledger could not resolve it.</summary>
    public string Reason { get; set; } = string.Empty;
}
