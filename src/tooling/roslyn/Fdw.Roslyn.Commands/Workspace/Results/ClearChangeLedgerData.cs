using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// The outcome of a <c>ClearChangeLedger</c> run.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ClearChangeLedgerData
{
    /// <summary>Gets or sets why the history was discarded.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Gets or sets how many entries were discarded.</summary>
    public int EntriesDiscarded { get; set; }
}
