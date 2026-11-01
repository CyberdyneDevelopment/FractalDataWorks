using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to re-home specific types into the namespace they should declare.
/// </summary>
/// <remarks>
/// The counterpart to <see cref="MoveNamespaceCommand"/>, and NOT the same operation. MoveNamespace says
/// "this namespace is misnamed" and moves everything in it. This says "these particular types declare the
/// wrong namespace" and moves only them, leaving every other type that legitimately shares the old
/// namespace exactly where it is.
/// </remarks>
[TypeOption(typeof(RoslynCommands), "MoveTypesToNamespace")]
public sealed class MoveTypesToNamespaceCommand : RoslynCommandBase, IReasonedCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTypesToNamespaceCommand"/> class.
    /// </summary>
    public MoveTypesToNamespaceCommand()
        : base("MoveTypesToNamespace", RoslynCommandCategories.Refactoring, "Re-home SPECIFIC types into the namespace they should declare, selected by file. Use when a handful of files declare the wrong namespace while other files legitimately share that same namespace — MoveNamespace cannot express this, because it renames a namespace everywhere it appears and would drag the correct files along too. Only the selected files' declarations change; references to the moved types are followed (qualified names rewritten, a using added where the reference was unqualified) while references to types that stayed are left alone. This is the location-stays-name-changes mirror of MoveTypeToProject, and it is CONSUMER-BREAKING for the moved types because their fully-qualified name changes, along with any [TypeOption] Id derived from it. Previews what it breaks with each problem attributed to the type causing it; SkipTypes excludes offenders so the rest still moves; a real run refuses if the result would not compile.")
    {
    }

    /// <summary>
    /// Gets or sets the files whose types should be re-homed.
    /// </summary>
    /// <remarks>
    /// Selection is by FILE because that is what FindNamespaceMismatches reports and what a reviewer
    /// triages, and because one-type-per-file makes a file the natural unit here.
    /// </remarks>
    public string[] FilePaths { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the namespace the selected types should declare.
    /// </summary>
    public string NewNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets type names to exclude from the move.
    /// </summary>
    public string[] SkipTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether to preview only. Defaults to <see langword="true"/>.
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to proceed when the probe reports problems it may not be
    /// able to see the truth about. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// For the case where the CALLER can see something the probe cannot: a reference the workspace never
    /// resolved, a generator's output, a project outside the loaded solution. The probe compiles what it
    /// was given, so "this would not compile" is only ever a statement about the loaded solution — and a
    /// solution that is missing a reference will say a change breaks things when it does not.
    ///
    /// It is defaulted OFF, so nothing changes for a caller who does not set it, and the check still runs
    /// and still reports everything it found — the override only decides whether those findings STOP the
    /// change. Setting it is itself the deliberate choice, which is what separates this from a silent
    /// fallback; deleting the check instead would let a genuinely broken rewrite land with nothing said.
    /// Supply Reason as well when you want the justification recorded in the change ledger.
    /// </remarks>
    public bool AcceptUnverified { get; set; }

    /// <summary>
    /// Gets or sets why this change is being made, recorded verbatim into the change ledger.
    /// </summary>
    public string? Reason { get; set; }
}
