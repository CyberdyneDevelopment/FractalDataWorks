using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to find concrete implementations belonging to a family.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindFamilyImplementations")]
public sealed class FindFamilyImplementationsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindFamilyImplementationsCommand"/> class.
    /// </summary>
    public FindFamilyImplementationsCommand()
        : base("FindFamilyImplementations", RoslynCommandCategories.Search, "Enumerate concrete implementations of a family root, optionally filtered by a NamespaceFilter glob (e.g. `*.Services.Connections.*`). Use to scope a drift analysis to a specific domain and count how much surface each implementation adds beyond the root contract; abstract implementations are excluded by default. Returns a list of FamilyImplementation entries with own-member counts, namespace, and file/line.")
    {
    }

    /// <summary>
    /// Gets or sets the fully qualified or simple name of the root type.
    /// </summary>
    [System.ComponentModel.Description("Fully qualified or simple type name of the family root.")]
    public string RootTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional glob pattern (e.g. "*.Services.Connections.*")
    /// used to restrict results to a namespace domain.
    /// </summary>
    [System.ComponentModel.Description("Optional namespace glob (e.g. '*.Services.Connections.*') to restrict results to a specific domain.")]
    public string? NamespaceFilter { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to include abstract implementations.
    /// </summary>
    [System.ComponentModel.Description("When true, include abstract implementations; false (default) returns only concrete classes.")]
    public bool IncludeAbstract { get; init; }
}
