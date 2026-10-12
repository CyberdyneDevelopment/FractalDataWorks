using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to find all MessageLogging attribute usages in the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindMessageLogging")]
public sealed class FindMessageLoggingCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindMessageLoggingCommand"/> class.
    /// </summary>
    public FindMessageLoggingCommand()
        : base("FindMessageLogging", RoslynCommandCategories.Conventions, "Find every usage of the FDW [MessageLogging] attribute and the methods source-generated from it. Use to enumerate the diagnostic surface a project exposes or to audit for consistency. Pass ProjectFilter to narrow scope. Returns a list of MessageLogging sites with file/line and the generated method signature.")
    {
    }
    /// <summary>
    /// Gets or sets the optional project filter.
    /// </summary>
    [System.ComponentModel.Description("Optional glob pattern to scope the search to specific projects. Null/empty searches the whole solution.")]
    public string? ProjectFilter { get; init; }
}
