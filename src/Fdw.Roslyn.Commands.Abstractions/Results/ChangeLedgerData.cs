using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Query data returned by GetChangeLedger, summarizing the session's recorded changes.
/// </summary>
public sealed class ChangeLedgerData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeLedgerData"/> class.
    /// </summary>
    public ChangeLedgerData(
        IReadOnlyList<ChangeLedgerEntry> entries,
        int totalEntries,
        int renameCount,
        int moveCount,
        int addedCount,
        int removedCount)
    {
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        TotalEntries = totalEntries;
        RenameCount = renameCount;
        MoveCount = moveCount;
        AddedCount = addedCount;
        RemovedCount = removedCount;
    }

    /// <summary>
    /// Gets the recorded ledger entries.
    /// </summary>
    public IReadOnlyList<ChangeLedgerEntry> Entries { get; }

    /// <summary>
    /// Gets the total number of ledger entries.
    /// </summary>
    public int TotalEntries { get; }

    /// <summary>
    /// Gets the number of rename symbol changes across all entries.
    /// </summary>
    public int RenameCount { get; }

    /// <summary>
    /// Gets the number of move symbol changes across all entries.
    /// </summary>
    public int MoveCount { get; }

    /// <summary>
    /// Gets the number of added symbol changes across all entries.
    /// </summary>
    public int AddedCount { get; }

    /// <summary>
    /// Gets the number of removed symbol changes across all entries.
    /// </summary>
    public int RemovedCount { get; }
}
