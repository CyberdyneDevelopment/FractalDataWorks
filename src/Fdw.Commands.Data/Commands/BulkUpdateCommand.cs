using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Expansions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.Updates;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Bulk update command: each source row updates the row its key column values select. Returns the number of rows
/// updated. A connection with no bulk mechanism for it runs an <see cref="UpdateCommand{T}"/> for each row
/// (<see cref="IExpandableDataCommand"/>).
/// </summary>
/// <typeparam name="T">The type of the source rows (a dictionary of column values or an entity).</typeparam>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "BulkUpdate", RestrictToCurrentCompilation = true)]
public sealed class BulkUpdateCommand<T> : DataCommandBase<int, IEnumerable<T>>, IExpandableDataCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkUpdateCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The source rows.</param>
    /// <param name="options">The key columns that select the row each source row updates.</param>
    public BulkUpdateCommand(IEnumerable<T> data, BulkUpdateOptions options)
        : base("BulkUpdate", data)
    {
        Options = options;
        BulkUpdateCommandLog.CommandCreated(NullLogger<BulkUpdateCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>Gets the key columns that select the row each source row updates.</summary>
    public BulkUpdateOptions Options { get; }

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container)
    {
        var commands = new List<ExpandedCommand>();
        foreach (var row in Data)
        {
            var filter = KeyFilters.ForRow(CommandType, Options.KeyColumns, row);
            if (filter.IsFailure)
                return filter.ToNewResult<IReadOnlyList<ExpandedCommand>>();

            commands.Add(new ExpandedCommand(new UpdateCommand<T>(row) { Filter = filter.Value }));
        }

        CommandExpansionLog.Expanded(NullLogger<BulkUpdateCommand<T>>.Instance, CommandType, commands.Count);
        return GenericResult<IReadOnlyList<ExpandedCommand>>.Success(commands);
    }
}
