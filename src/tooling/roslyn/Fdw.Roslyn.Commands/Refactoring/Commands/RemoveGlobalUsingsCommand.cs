using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to remove global using directives and give an explicit using to every file that relied on them.
/// </summary>
/// <remarks>
/// A global using's scope is exactly one compilation, so the PROJECT is both the unit of work and the
/// blast radius: deleting one directive changes name resolution for every file in that project. There is
/// no coherent solution-wide run, only N independent per-project ones.
/// </remarks>
[TypeOption(typeof(RoslynCommands), "RemoveGlobalUsings")]
public sealed class RemoveGlobalUsingsCommand : RoslynCommandBase, IReasonedCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGlobalUsingsCommand"/> class.
    /// </summary>
    public RemoveGlobalUsingsCommand()
        : base("RemoveGlobalUsings", RoslynCommandCategories.Refactoring, "Remove global using directives from ONE project and give an explicit using to every file that relied on them, so imports become visible at the file that needs them. Uses the compiler as the oracle rather than reimplementing name resolution: it compiles the project, removes the directives, recompiles, and treats the newly-appeared diagnostics as the exact set of files to repair — which also catches the reverse case where removing an import RESOLVES an ambiguity. Refuses a namespace that MSBuild also supplies via ImplicitUsings or <Using Include>, because deleting that source line changes nothing and the SDK regenerates it next build; the failure names the props file to edit instead. Refuses outright if a break lands in a generated file, since editing it would be discarded. A real run re-compiles after repair and reverts rather than leave a half-fixed project.")
    {
    }

    /// <summary>
    /// Gets or sets the project whose global usings should be removed.
    /// </summary>
    /// <remarks>
    /// Required, and singular. A global using is compilation-scoped, so asking for "the solution" would be
    /// asking for an unreviewable change with a blast radius nobody can hold in their head.
    /// </remarks>
    public string Project { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespaces whose global using directives should be removed.
    /// </summary>
    /// <remarks>
    /// Required and non-empty on purpose. An "all global usings" default would be the "if missing then
    /// assume X" branch the first rule forbids, and it is what makes a plan unreviewable.
    /// </remarks>
    public string[] Namespaces { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether to preview only. Defaults to <see langword="true"/>.
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to proceed when the project's compilation cannot be
    /// verified. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// The whole algorithm here IS a diagnostic diff, so without a bindable baseline there is nothing to
    /// compare against and the removal cannot be checked at all. Setting this proceeds anyway — removing
    /// the directives and inserting the explicit imports without the compiler confirming the result — for
    /// the case where the caller can see something the loaded solution cannot.
    /// </remarks>
    public bool AcceptUnverified { get; set; }

    /// <summary>
    /// Gets or sets why this change is being made, recorded verbatim into the change ledger.
    /// </summary>
    public string? Reason { get; set; }
}
