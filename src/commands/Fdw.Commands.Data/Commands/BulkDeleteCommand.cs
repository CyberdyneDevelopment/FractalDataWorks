using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.Expansions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Bulk delete command: each source row removes the row its key column values select, or flags it deleted when a soft delete is
/// set. Returns the number of rows deleted (or flagged). A database connection translates it to one set-based statement for the
/// whole batch (FDW-4954); a kind with no translator for it runs a <see cref="DeleteCommand"/> for each row
/// (<see cref="IExpandableDataCommand"/>).
/// </summary>
/// <typeparam name="T">The type of the source rows (a dictionary of column values or an entity), which carry the key columns.</typeparam>
/// <remarks>
/// A soft delete flags the matched rows its mode selects: those whose marker column holds the marker value, or every matched row.
/// Flagging the keys absent from a source set is a <see cref="DeleteCommand"/> with an absent-key set, not a bulk delete.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "BulkDelete", RestrictToCurrentCompilation = true)]
public sealed class BulkDeleteCommand<T> : DataCommandBase<int, IEnumerable<T>>, ISetBasedRowsCommand, IBulkDeleteCommand, IExpandableDataCommand, ISoftDeletableCommand, IReportsUnmatchedKeys
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkDeleteCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The source rows, which carry the key columns.</param>
    /// <param name="options">The key columns that select the row each source row deletes.</param>
    public BulkDeleteCommand(IEnumerable<T> data, BulkDeleteOptions options)
        : base("BulkDelete", data)
    {
        Options = options;
        BulkDeleteCommandLog.CommandCreated(NullLogger<BulkDeleteCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>Gets the key columns that select the row each source row deletes.</summary>
    public BulkDeleteOptions Options { get; }

    /// <summary>
    /// Gets the soft-delete options. When set, the matched rows are flagged deleted instead of removed, as
    /// <see cref="DeleteCommand.SoftDelete"/> does for the rows a filter matches.
    /// </summary>
    public SoftDeleteOptions? SoftDelete { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Null (the default for a command built without it) means the command does not report its unmatched keys and answers a row count. A caller that wants
    /// them states how many keys the answer carries, 0 for only the count, and runs the command with <c>Execute&lt;BulkWriteAnswer&gt;</c> (FDW-4988).
    /// </remarks>
    public int? UnmatchedKeySample { get; init; }

    /// <inheritdoc/>
    public int RowCount => Data.Count();

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container)
    {
        var commands = new List<ExpandedCommand>();
        foreach (var row in Data)
        {
            var filter = KeyFilters.ForRow(CommandType, Options.KeyColumns, row);
            if (filter.IsFailure)
                return filter.ToNewResult<IReadOnlyList<ExpandedCommand>>();

            commands.Add(new ExpandedCommand(new DeleteCommand { Filter = filter.Value, SoftDelete = SoftDelete }));
        }

        CommandExpansionLog.Expanded(NullLogger<BulkDeleteCommand<T>>.Instance, CommandType, commands.Count);
        return GenericResult<IReadOnlyList<ExpandedCommand>>.Success(commands);
    }
}
