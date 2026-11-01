using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.Upserts;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Bulk upsert command: the upsert for large loads. Same meaning as <see cref="UpsertCommand{T}"/>; it declares the
/// intent so a connection can use its bulk mechanism. Returns the number of rows written. A connection with no bulk
/// mechanism for it runs an <see cref="UpsertCommand{T}"/> for each row (<see cref="IExpandableDataCommand"/>).
/// </summary>
/// <typeparam name="T">The type of the source rows (a dictionary of column values or an entity).</typeparam>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "BulkUpsert", RestrictToCurrentCompilation = true)]
public sealed class BulkUpsertCommand<T> : DataCommandBase<int, IEnumerable<T>>, IUpsertCommand, IBulkExpandableDataCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkUpsertCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The source rows.</param>
    /// <param name="options">The key, compare and write columns and optional behaviours.</param>
    public BulkUpsertCommand(IEnumerable<T> data, UpsertOptions options)
        : base("BulkUpsert", data)
    {
        Options = options;
        BulkUpsertCommandLog.CommandCreated(NullLogger<BulkUpsertCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>Gets the key, compare and write columns and the optional versioning and insert-only behaviour.</summary>
    public UpsertOptions Options { get; }

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container)
        => ExpandRows([.. Data], null);

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container, int maxSerialInsert)
        => ExpandRows([.. Data], maxSerialInsert);

    // Above maxSerialInsert the rows are one upsert, which replaces them together; up to it (or when the connection states none)
    // each row is an upsert of its own.
    private IGenericResult<IReadOnlyList<ExpandedCommand>> ExpandRows(List<T> rows, int? maxSerialInsert)
    {
        List<ExpandedCommand> commands = maxSerialInsert is { } max && rows.Count > max
            ? [new ExpandedCommand(new UpsertCommand<T>(rows, Options))]
            : rows.Select(row => new ExpandedCommand(new UpsertCommand<T>([row], Options))).ToList();
        CommandExpansionLog.Expanded(NullLogger<BulkUpsertCommand<T>>.Instance, CommandType, commands.Count);
        return GenericResult<IReadOnlyList<ExpandedCommand>>.Success(commands);
    }
}
