using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Expansions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.Upserts;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Upsert command: writes the rows into the target, inserting keys that do not exist and updating (or, with
/// versioning, closing and re-opening) keys whose compare columns changed. Returns the number of rows written.
/// </summary>
/// <typeparam name="T">The type of the source rows (a dictionary of column values or an entity).</typeparam>
/// <remarks>
/// <para>
/// The caller passes everything as configuration (<see cref="UpsertOptions"/>); nothing here names a table or
/// column. Translators turn it into one set-based statement per connection type. Rows that already match on every
/// compare column are left untouched and are not counted. The command holds one row or many.
/// </para>
/// <para>
/// A kind with no upsert translator runs it as a delete by key and an insert for each row (<see cref="IExpandableDataCommand"/>):
/// each matching row is replaced by the row given, a row with no match is inserted, and the rows written are the rows
/// inserted. That does not honour compare columns (a matching row is always replaced and counted), versioning or value
/// sources, which it refuses, nor a container column the rows do not carry, which the replacement would lose.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Upsert", RestrictToCurrentCompilation = true)]
public sealed class UpsertCommand<T> : DataCommandBase<int, IEnumerable<T>>, IUpsertCommand, IBulkExpandableDataCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The source rows.</param>
    /// <param name="options">The key, compare and write columns and optional behaviours.</param>
    public UpsertCommand(IEnumerable<T> data, UpsertOptions options)
        : base("Upsert", data)
    {
        Options = options;
        UpsertCommandLog.CommandCreated(NullLogger<UpsertCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>Gets the key, compare and write columns and the optional versioning and insert-only behaviour.</summary>
    public UpsertOptions Options { get; }

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container)
        => ExpandRows(container, [.. Data], null);

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container, int maxSerialInsert)
        => ExpandRows(container, [.. Data], maxSerialInsert);

    // Up to maxSerialInsert rows (or when the connection states none) each row is replaced on its own, a delete by key and an
    // insert. Above it the rows are replaced together: the deletes by key in groups, then one insert of every row, which the
    // connection takes the bulk way because the rows outnumber maxSerialInsert.
    private IGenericResult<IReadOnlyList<ExpandedCommand>> ExpandRows(IStorageContainer container, List<T> rows, int? maxSerialInsert)
    {
        var refusal = Refusal(container);
        if (refusal is not null)
            return GenericResult<IReadOnlyList<ExpandedCommand>>.Failure(
                CommandExpansionLog.NotComposable(NullLogger<UpsertCommand<T>>.Instance, CommandType, refusal));

        var steps = new List<ExpandedCommand>();
        if (maxSerialInsert is { } max && rows.Count > max)
        {
            var filters = KeyFilters.ForRows(CommandType, Options.KeyColumns, rows, max);
            if (filters.IsFailure)
                return filters.ToNewResult<IReadOnlyList<ExpandedCommand>>();

            steps.AddRange(filters.Value!.Select(filter => new ExpandedCommand(new DeleteCommand { Filter = filter }, CountsAsAffected: false)));
            steps.Add(new ExpandedCommand(new InsertCommand<T>((IEnumerable<T>)rows)));
        }
        else
        {
            foreach (var row in rows)
            {
                var filter = KeyFilters.ForRow(CommandType, Options.KeyColumns, row);
                if (filter.IsFailure)
                    return filter.ToNewResult<IReadOnlyList<ExpandedCommand>>();

                steps.Add(new ExpandedCommand(new DeleteCommand { Filter = filter.Value }, CountsAsAffected: false));
                steps.Add(new ExpandedCommand(new InsertCommand<T>(row)));
            }
        }

        CommandExpansionLog.Expanded(NullLogger<UpsertCommand<T>>.Instance, CommandType, steps.Count);
        return GenericResult<IReadOnlyList<ExpandedCommand>>.Success(steps);
    }

    // What a delete and an insert cannot do for these options and this container, or nothing when they can stand in.
    private string? Refusal(IStorageContainer container)
    {
        if (Options.Versioning is not null)
            return "versioning keeps history, which deleting the row would erase";
        if (Options.ValueSources is { Count: > 0 })
            return "its value sources are supplied by the database, which an insert of the row does not do";

        // A container whose columns are known: replacing the row writes only the columns the rows carry, so a column the
        // rows do not carry would lose its value. A container with no known columns (a file) cannot be checked.
        var lost = container is IDataContainer { Schema: { } schema }
            ? schema.Fields
                .Where(field => !field.IsIdentity && !field.IsComputed && !field.IsSystemProvided)
                .Select(field => field.Name)
                .FirstOrDefault(name => !Options.WriteColumns.Contains(name, System.StringComparer.OrdinalIgnoreCase)
                    && !Options.KeyColumns.Contains(name, System.StringComparer.OrdinalIgnoreCase))
            : null;
        return lost is null ? null : $"replacing a row would lose column '{lost}', which the rows do not carry";
    }
}
