using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to validate proper Result handling patterns (checking IsSuccess before accessing Value).
/// </summary>
[TypeOption(typeof(RoslynCommands), "ValidateResultHandling")]
public sealed class ValidateResultHandlingCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateResultHandlingCommand"/> class.
    /// </summary>
    public ValidateResultHandlingCommand()
        : base("ValidateResultHandling", RoslynCommandCategories.Conventions, "Find call sites that consume an IGenericResult-returning method but ignore the result (no .IsSuccess check, no Match, etc.). Use as the second half of the FDW Result-pattern audit — these are the places that silently swallow failure. Pass ProjectFilter to narrow scope. Returns a list of mishandled call sites with file/line.")
    {
    }
    /// <summary>
    /// Gets or sets the optional project filter.
    /// </summary>
    [System.ComponentModel.Description("Optional glob pattern to scope the audit to specific projects. Null/empty audits the whole solution.")]
    public string? ProjectFilter { get; init; }
}
