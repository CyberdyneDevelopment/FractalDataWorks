using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// A source generator the moved code needs referenced in order to be complete.
/// </summary>
// Why: pure data holder, auto-properties only, no logic
[ExcludeFromCodeCoverage]
public sealed class GeneratorRequirement
{
    /// <summary>Gets or sets the generator project that must be referenced as an analyzer.</summary>
    public string GeneratorProject { get; set; } = string.Empty;

    /// <summary>Gets or sets the attribute occurrence that demands it.</summary>
    public string BecauseOf { get; set; } = string.Empty;
}
