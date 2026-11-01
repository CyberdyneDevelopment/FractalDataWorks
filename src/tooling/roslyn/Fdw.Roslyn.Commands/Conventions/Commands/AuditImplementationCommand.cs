using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to check a project against the implementation guides that apply to it.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AuditImplementation")]
public sealed class AuditImplementationCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditImplementationCommand"/> class.
    /// </summary>
    public AuditImplementationCommand()
        : base("AuditImplementation", RoslynCommandCategories.Conventions, "Check a ProjectName against the implementation guides that apply to it: references the guide forbids, references it requires and the project lacks, and, when TypeName is given, members of the guide's abstractions the type leaves unimplemented. Each finding cites the rule it rests on. The guide's must and must-not statements that cannot be evaluated mechanically are returned as ManualChecks for review. FDW's analyzers run only inside fractaldataworks, so for a downstream project this audit is the check. Fails with NoImplementationGuide when no guide applies.")
    {
    }

    /// <summary>
    /// Gets or sets the path to the folder holding implementing.json.
    /// </summary>
    [System.ComponentModel.Description("Required. Path to the fdw-anatomy content folder that holds implementing.json and implementing.meta.json.")]
    public string? GuidesPath { get; init; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    [System.ComponentModel.Description("Required. The project to audit.")]
    public string? ProjectName { get; init; }

    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    [System.ComponentModel.Description("Optional. A type in the project to also check for unimplemented members of the guide's abstractions.")]
    public string? TypeName { get; init; }
}
