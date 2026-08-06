using System;
using System.Text.Json.Serialization;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to repair unresolved-reference build errors using the session change ledger.
/// </summary>
[TypeOption(typeof(RoslynCommands), "RepairMovedReferences")]
public sealed class RepairMovedReferencesCommand : RoslynCommandBase, ILedgerAwareCommand, IReasonedCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepairMovedReferencesCommand"/> class.
    /// </summary>
    public RepairMovedReferencesCommand()
        : base("RepairMovedReferences", RoslynCommandCategories.Workspace, "Repair CS0246/CS0234 'type or namespace not found' errors caused by types that moved between assemblies. Two sources: the session change ledger (you did the moves) or GuidePath, a published migration guide (you are a CONSUMER who bumped a version and is now fixing the fallout) — the guide is read from its assembly-move table, so a consumer needs no ledger. Only works because a move leaves the fully-qualified name unchanged; a MoveNamespace rename is a real consumer break and is deliberately NOT auto-repaired. Errors neither source explains are reported as unresolved with a reason, never guessed at. Writing is opt-in via WriteToDisk and gated on approval: PreviewPath writes a plan you prune (deleting a line rejects it) and ApplyFromPath processes what survives, or use ApproveAll with Reject. Under central package management the version goes in Directory.Packages.props; VersionPin takes a literal or a property such as $(FdwVersion). Returns the repairs, what was written, and every unresolved error.")
    {
    }

    /// <summary>
    /// Gets or sets the project name or path fragment to repair. Omit to repair the whole solution.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preview only. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// The preview is the list you review. Every repair carries a stable Id; approve them on the next
    /// call with ApproveAll or Approve, and veto individual ones with Reject.
    /// </remarks>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether every proposed repair is approved.
    /// </summary>
    /// <remarks>
    /// This is the thumbs-up-everything mode; pair it with <see cref="Reject"/> to opt in broadly while
    /// explicitly vetoing individual repairs.
    /// </remarks>
    public bool ApproveAll { get; set; }

    /// <summary>
    /// Gets or sets the repair ids to approve. Ignored when <see cref="ApproveAll"/> is set.
    /// </summary>
    public string[] Approve { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the repair ids to reject. Rejection always wins over approval.
    /// </summary>
    public string[] Reject { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether approved repairs are written to the project files on disk.
    /// </summary>
    /// <remarks>
    /// Opt-in. Everything else in this server mutates an in-memory solution and persists through
    /// ApplyWorkspaceChanges, but ApplyWorkspaceChanges writes document text only and never touches a
    /// .csproj — so without this the reference fix exists nowhere but memory.
    /// </remarks>
    public bool WriteToDisk { get; set; }

    /// <summary>
    /// Gets or sets the version to pin a PackageReference at — a literal such as "1.0.0-rc.1" or an
    /// MSBuild property such as "$(FdwVersion)".
    /// </summary>
    /// <remarks>
    /// Required only when a repair resolves to a package rather than a project in the solution. There is
    /// no default: guessing a consumer's version pin is exactly the kind of silent choice that produces a
    /// build that restores the wrong thing.
    /// </remarks>
    public string? VersionPin { get; set; }

    /// <summary>
    /// Gets or sets a path to write the hand-editable repair plan to during a preview.
    /// </summary>
    /// <remarks>
    /// Relative paths resolve against the SOLUTION directory, so the plan can live in the repo alongside
    /// the migration guide. Prune the lines you do not want, then re-run with ApplyFromPath.
    /// </remarks>
    public string? PreviewPath { get; set; }

    /// <summary>
    /// Gets or sets a pruned plan file to take approvals from.
    /// </summary>
    /// <remarks>
    /// What remains in the file is what gets applied — deleting a line is the rejection. Supersedes
    /// ApproveAll/Approve, because a reviewed file is a more explicit instruction than a blanket flag.
    /// </remarks>
    public string? ApplyFromPath { get; set; }

    /// <summary>
    /// Gets or sets a published migration guide to take the type-to-package mapping from, instead of the
    /// session change ledger.
    /// </summary>
    /// <remarks>
    /// This is the CONSUMER entry point. A consumer never ran the moves, so has no session ledger — they
    /// have the producer's committed guide. Relative paths resolve against the SOLUTION directory. Every
    /// section of an appended guide is read, because a consumer jumping several versions needs all of
    /// them, not just the newest.
    /// </remarks>
    public string? GuidePath { get; set; }

    /// <summary>
    /// Gets or sets why this change is being made, recorded verbatim into the change ledger.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the change ledger, injected by the command handler.
    /// </summary>
    [JsonIgnore]
    public IChangeLedger? Ledger { get; set; }
}
