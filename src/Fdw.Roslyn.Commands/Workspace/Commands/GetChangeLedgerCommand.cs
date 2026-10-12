using System.Text.Json.Serialization;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to get the session's recorded change ledger.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetChangeLedger")]
public sealed class GetChangeLedgerCommand : RoslynCommandBase, ILedgerAwareCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetChangeLedgerCommand"/> class.
    /// </summary>
    public GetChangeLedgerCommand()
        : base("GetChangeLedger", RoslynCommandCategories.Workspace, "Return the session's recorded change ledger: every mutating command executed since the last SetBaseline, with renamed/moved/added/removed symbol counts. Use to review what has changed before writing a migration guide. Returns ledger entries and counts.")
    {
    }

    /// <summary>
    /// Gets or sets the change ledger. Set by the handler before translation; excluded from JSON
    /// because the concrete ledger implementation is not a serializable data value.
    /// </summary>
    [JsonIgnore]
    public IChangeLedger? Ledger { get; set; }
}
