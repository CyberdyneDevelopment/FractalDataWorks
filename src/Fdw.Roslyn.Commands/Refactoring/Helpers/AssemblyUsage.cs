using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Helpers;

/// <summary>
/// How much a document depends on one assembly.
/// </summary>
// Why: pure data holder, auto-properties only, no logic
[ExcludeFromCodeCoverage]
public sealed class AssemblyUsage
{
    /// <summary>Gets or sets the assembly name.</summary>
    public string Assembly { get; set; } = string.Empty;

    /// <summary>Gets or sets how many distinct symbols resolved to this assembly.</summary>
    public int SymbolCount { get; set; }

    /// <summary>Gets or sets one representative symbol that creates the dependency.</summary>
    public string ExampleSymbol { get; set; } = string.Empty;
}
