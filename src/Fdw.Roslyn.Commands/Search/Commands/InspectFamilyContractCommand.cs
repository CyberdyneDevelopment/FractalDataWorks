using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to inspect the public surface of a family root type (interface or abstract base).
/// </summary>
[TypeOption(typeof(RoslynCommands), "InspectFamilyContract")]
public sealed class InspectFamilyContractCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InspectFamilyContractCommand"/> class.
    /// </summary>
    public InspectFamilyContractCommand()
        : base("InspectFamilyContract", RoslynCommandCategories.Search, "Read the public surface of a family root type (interface or abstract base): members with signatures and modifiers, generic parameters with constraints, base chain, and directly implemented interfaces. Use as the first step of a top-down family audit to confirm the canonical contract before checking derived types or implementations for drift. Returns a single FamilyContract object.")
    {
    }

    /// <summary>
    /// Gets or sets the fully qualified or simple name of the root type to inspect.
    /// </summary>
    [System.ComponentModel.Description("Fully qualified or simple type name of the family root to inspect.")]
    public string TypeName { get; init; } = string.Empty;
}
