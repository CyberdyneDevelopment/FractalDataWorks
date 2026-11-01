using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to discard the recorded change history.
/// </summary>
/// <remarks>
/// The ledger used to be cleared as a side effect of load_solution, close_workspace, close_all and
/// SetBaseline — four operations that have nothing to do with wanting the history gone, and no way to
/// ask for it deliberately. Reloading a solution destroying the record the migration guide is built
/// from is the opposite of what any of those callers wanted.
/// </remarks>
[TypeOption(typeof(RoslynCommands), "ClearChangeLedger")]
public sealed class ClearChangeLedgerCommand : RoslynCommandBase, ILedgerClearingCommand, IReasonedCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearChangeLedgerCommand"/> class.
    /// </summary>
    public ClearChangeLedgerCommand()
        : base("ClearChangeLedger", RoslynCommandCategories.Workspace, "Discard the recorded change history. The ledger is what WriteMigrationGuide and RepairMovedReferences read, so clearing it throws away the record consumers migrate against — which is why it now only happens when asked for explicitly, and no longer as a side effect of loading a solution, closing a workspace, or setting a baseline.")
    {
    }

    /// <summary>
    /// Gets or sets why the history is being discarded, recorded before the clear takes effect.
    /// </summary>
    public string? Reason { get; set; }
}
