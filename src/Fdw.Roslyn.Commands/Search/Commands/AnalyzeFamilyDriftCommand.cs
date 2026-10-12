using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to analyze structural drift across implementations of a family root.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AnalyzeFamilyDrift")]
public sealed class AnalyzeFamilyDriftCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeFamilyDriftCommand"/> class.
    /// </summary>
    public AnalyzeFamilyDriftCommand()
        : base("AnalyzeFamilyDrift", RoslynCommandCategories.Search, "Diff the public surfaces of every concrete implementation of a family root and bucket each divergent member as Hoist (in every implementation — promote to base), MostHave (in N-1 — fix outliers), Bloat (in only one — likely rogue addition), or Mixed. Run as the third step of a top-down family audit, after InspectFamilyContract and FindFamilyImplementations. Returns a FamilyDriftReport with grouped drift members plus any extension methods whose `this` parameter targets the family.")
    {
    }

    /// <summary>
    /// Gets or sets the fully qualified or simple name of the root type.
    /// </summary>
    [System.ComponentModel.Description("Fully qualified or simple type name of the family root (interface or abstract class) to analyze.")]
    public string RootTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional glob pattern used to restrict the implementation set
    /// (e.g. "*.Services.Connections.*").
    /// </summary>
    [System.ComponentModel.Description("Optional namespace glob (e.g. '*.Services.Connections.*') to scope the implementation set to a specific domain.")]
    public string? NamespaceFilter { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to also scan for extension methods
    /// whose <c>this</c> parameter targets the family.
    /// </summary>
    [System.ComponentModel.Description("When true (default), also scan for extension methods whose `this` parameter targets the family — often a signal of workaround code that bypasses the canonical pattern.")]
    public bool IncludeExtensionMethods { get; init; } = true;
}
