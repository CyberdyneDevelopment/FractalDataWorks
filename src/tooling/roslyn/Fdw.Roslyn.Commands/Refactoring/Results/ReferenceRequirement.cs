using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// One assembly a project must reference, together with why.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceRequirement
{
    /// <summary>Gets or sets the assembly name.</summary>
    public string Assembly { get; set; } = string.Empty;

    /// <summary>Gets or sets a representative type that creates the requirement.</summary>
    public string BecauseOf { get; set; } = string.Empty;

    /// <summary>Gets or sets how many distinct referenced symbols resolved to this assembly.</summary>
    public int SymbolCount { get; set; }
}
