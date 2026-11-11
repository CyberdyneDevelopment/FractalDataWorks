using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Expansions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Commands.Data.Updates;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Bulk update command: each source row updates the row its key column values select, writing the write columns. Returns
/// the number of rows updated. A database connection translates it to one set-based statement for the whole batch
/// (FDW-4953); a kind with no translator for it (an HTTP API) runs an <see cref="UpdateCommand{T}"/> for each row
/// (<see cref="IExpandableDataCommand"/>).
/// </summary>
/// <typeparam name="T">The type of the source rows (a dictionary of column values or an entity).</typeparam>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "BulkUpdate", RestrictToCurrentCompilation = true)]
public sealed class BulkUpdateCommand<T> : DataCommandBase<int, IEnumerable<T>>, ISetBasedRowsCommand, IBulkUpdateCommand, IExpandableDataCommand, ISoftDeletableCommand, IReportsUnmatchedKeys
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkUpdateCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The source rows.</param>
    /// <param name="options">The key columns that select the row each source row updates, and the columns written.</param>
    public BulkUpdateCommand(IEnumerable<T> data, BulkUpdateOptions options)
        : base("BulkUpdate", data)
    {
        Options = options;
        BulkUpdateCommandLog.CommandCreated(NullLogger<BulkUpdateCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>Gets the key columns that select the row each source row updates, and the columns written.</summary>
    public BulkUpdateOptions Options { get; }

    /// <summary>
    /// Gets the soft-delete options. When set, the update also flags the matched rows the marker selects deleted, in the
    /// same statement, as <see cref="UpdateCommand{T}.SoftDelete"/> does for one row.
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

            commands.Add(new ExpandedCommand(new UpdateCommand<T>(row) { Filter = filter.Value, SoftDelete = SoftDelete }));
        }

        CommandExpansionLog.Expanded(NullLogger<BulkUpdateCommand<T>>.Instance, CommandType, commands.Count);
        return GenericResult<IReadOnlyList<ExpandedCommand>>.Success(commands);
    }
}
