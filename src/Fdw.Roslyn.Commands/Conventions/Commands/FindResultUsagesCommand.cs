using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to find IGenericResult usage patterns in the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindResultUsages")]
public sealed class FindResultUsagesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindResultUsagesCommand"/> class.
    /// </summary>
    public FindResultUsagesCommand()
        : base("FindResultUsages", RoslynCommandCategories.Conventions, "Find every method or property whose return type implements IGenericResult, the FDW Result pattern. Use to audit Result-pattern adoption — methods that should return Result but don't will not appear here. Pass ProjectFilter to narrow scope. Returns ResultUsageInfo entries.")
    {
    }
    /// <summary>
    /// Gets or sets the optional project filter.
    /// </summary>
    [System.ComponentModel.Description("Optional glob pattern to scope the audit to specific projects. Null/empty audits the whole solution.")]
    public string? ProjectFilter { get; init; }
}
