#pragma warning disable CA1305 // Specify IFormatProvider - workspace commands use invariant strings

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for getting the session's recorded change ledger.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetChangeLedger")]
public sealed class GetChangeLedgerTranslator
    : RoslynCommandTranslatorBase<GetChangeLedgerCommand, QueryResult<ChangeLedgerData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetChangeLedgerTranslator"/> class.
    /// </summary>
    public GetChangeLedgerTranslator()
        : base("GetChangeLedgerTranslator", "Translates get change ledger commands")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<ChangeLedgerData>>> Translate(
        GetChangeLedgerCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        GetChangeLedgerTranslatorLog.Getting(Logger);

        if (command.Ledger is null)
        {
            GetChangeLedgerTranslatorLog.LedgerNotAvailable(Logger);
            return Task.FromResult(GenericResult<QueryResult<ChangeLedgerData>>.Failure(
                RoslynResultCodes.ByName("LedgerNotAvailable")));
        }

        var entries = command.Ledger.Entries;
        var symbolChanges = entries.SelectMany(e => e.SymbolChanges).ToList();

        var renameCount = symbolChanges.Count(s =>
            string.Equals(s.ChangeType, SymbolChangeTypes.Renamed.Name, StringComparison.Ordinal));
        var moveCount = symbolChanges.Count(s =>
            string.Equals(s.ChangeType, SymbolChangeTypes.Moved.Name, StringComparison.Ordinal));
        var addedCount = symbolChanges.Count(s =>
            string.Equals(s.ChangeType, SymbolChangeTypes.Added.Name, StringComparison.Ordinal));
        var removedCount = symbolChanges.Count(s =>
            string.Equals(s.ChangeType, SymbolChangeTypes.Removed.Name, StringComparison.Ordinal));

        var data = new ChangeLedgerData(entries, entries.Count, renameCount, moveCount, addedCount, removedCount);
        var result = new QueryResult<ChangeLedgerData>(
            $"Change ledger contains {entries.Count} entries",
            data);

        GetChangeLedgerTranslatorLog.Retrieved(Logger, entries.Count);

        return Task.FromResult<IGenericResult<QueryResult<ChangeLedgerData>>>(
            GenericResult<QueryResult<ChangeLedgerData>>.Success(result));
    }
}
