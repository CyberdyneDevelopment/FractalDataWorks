using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to move the types of a namespace into the project that namespace implies.
/// </summary>
[TypeOption(typeof(RoslynCommands), "MoveTypeToProject")]
public sealed class MoveTypeToProjectCommand : RoslynCommandBase, IReasonedCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTypeToProjectCommand"/> class.
    /// </summary>
    public MoveTypeToProjectCommand()
        : base("MoveTypeToProject", RoslynCommandCategories.Refactoring, "Move every document declaring a namespace into the project that namespace names, across project boundaries. Use when the type's NAME is right but its LOCATION is wrong; use MoveNamespace for the opposite. Not consumer-breaking: the fully-qualified name and every TypeOption Id are unchanged, so a consumer hitting CS0246 needs a package reference, not a code edit. Fails loud with the alternatives when no project of that name exists. Returns RequiredReferences (what the target must reference, derived from the symbol graph), DroppableReferences (what the source can shed — the payoff metric, reported honestly including zero) and Breaks, which flags [TypeOption] types whose module-initializer registration moves compilation. DryRun defaults true and never enters the change ledger.")
    {
    }

    /// <summary>
    /// Gets or sets the namespace whose documents should move.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project to move out of. Omit to move matching documents from every project.
    /// </summary>
    public string? SourceProject { get; set; }

    /// <summary>
    /// Gets or sets the destination project. Omit to use the project named exactly <see cref="Namespace"/>.
    /// </summary>
    public string? TargetProject { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preview only. Defaults to <see langword="true"/>.
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Gets or sets type names to exclude from the move.
    /// </summary>
    /// <remarks>
    /// The preview attributes every problem to the type that causes it, so a caller can fix those types
    /// and rerun, or skip them here and move the rest. Without this the only options are "move everything"
    /// or "move nothing", which makes a single bad type block an otherwise clean batch.
    /// </remarks>
    public string[] SkipTypes { get; set; } = Array.Empty<string>();

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
    /// Gets or sets a value indicating whether nested namespaces move too. Defaults to
    /// <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to true because "move Fdw.Data.MsSql" reads as the namespace and everything under it —
    /// that is what nesting means. Exact-only matching was the surprising behaviour, and it failed
    /// SILENTLY: the split reported success having moved 150 of 203 files, leaving .Results,
    /// .Configurations, .Logging and .Translators behind in the old project.
    ///
    /// Set false for the genuinely narrow case where a sub-namespace must stay put.
    /// </remarks>
    public bool IncludeSubNamespaces { get; set; } = true;

    /// <summary>
    /// Gets or sets why this change is being made, recorded verbatim into the change ledger.
    /// </summary>
    /// <remarks>
    /// The ledger records WHAT moved; without this it never records WHY. A migration guide that says a
    /// type changed package is useful; one that also says which slice or issue caused it is auditable.
    /// </remarks>
    public string? Reason { get; set; }
}
