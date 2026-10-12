using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// A single recorded entry in the change ledger, capturing one mutating command's effects.
/// </summary>
public sealed class ChangeLedgerEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeLedgerEntry"/> class.
    /// </summary>
    public ChangeLedgerEntry(
        int sequence,
        string commandName,
        string summary,
        IReadOnlyList<LedgerFileChange> fileChanges,
        IReadOnlyList<SymbolChange> symbolChanges,
        IReadOnlyList<PathChange> pathChanges)
    {
        Sequence = sequence;
        CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        FileChanges = fileChanges ?? throw new ArgumentNullException(nameof(fileChanges));
        SymbolChanges = symbolChanges ?? throw new ArgumentNullException(nameof(symbolChanges));
        PathChanges = pathChanges ?? throw new ArgumentNullException(nameof(pathChanges));
    }

    /// <summary>
    /// Gets the 1-based sequence number of this entry within the ledger.
    /// </summary>
    public int Sequence { get; }

    /// <summary>
    /// Gets the name of the command that produced this entry.
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// Gets the mutation's summary text.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Gets the file changes recorded for this entry.
    /// </summary>
    public IReadOnlyList<LedgerFileChange> FileChanges { get; }

    /// <summary>
    /// Gets the symbol changes recorded for this entry.
    /// </summary>
    public IReadOnlyList<SymbolChange> SymbolChanges { get; }

    /// <summary>
    /// Gets the path changes recorded for this entry.
    /// </summary>
    public IReadOnlyList<PathChange> PathChanges { get; }
}
