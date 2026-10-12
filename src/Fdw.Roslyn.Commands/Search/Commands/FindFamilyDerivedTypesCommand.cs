using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to find interfaces and abstract bases that derive from a family root.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindFamilyDerivedTypes")]
public sealed class FindFamilyDerivedTypesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindFamilyDerivedTypesCommand"/> class.
    /// </summary>
    public FindFamilyDerivedTypesCommand()
        : base("FindFamilyDerivedTypes", RoslynCommandCategories.Search, "Enumerate every interface or abstract class that derives from a family root, with the count and names of public members each adds beyond the root contract. Use as the second step of a family audit, after InspectFamilyContract — catches cases like 'an extended interface added three extra methods nobody else implements.' Returns a list of FamilyDerivedType entries with file/line for each.")
    {
    }

    /// <summary>
    /// Gets or sets the fully qualified or simple name of the root type.
    /// </summary>
    [System.ComponentModel.Description("Fully qualified or simple type name of the family root whose derivatives to enumerate.")]
    public string RootTypeName { get; init; } = string.Empty;
}
