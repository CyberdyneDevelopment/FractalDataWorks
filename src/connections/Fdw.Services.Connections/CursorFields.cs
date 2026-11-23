using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// The fields a connection's cursor builds its records over (FDW-6970): the container's own fields for a read of one container, and, for a query that
/// joins or stacks containers, the columns its projection returns, under the names it returns them by.
/// </summary>
/// <remarks>
/// <para>
/// A cursor reads each value of a record by its field's name. A query that joins or stacks containers returns columns the container it is sent to does
/// not declare (a joined side's fields under their prefix), so a cursor built over that container's fields would read none of them and every record would
/// lose the joined side.
/// </para>
/// <para>
/// Every column of such a query states its alias, the name it is returned by and the name the cursor reads it by. A column without one is refused, never
/// read by its field name in its place.
/// </para>
/// </remarks>
public static class CursorFields
{
    /// <summary>Gets the fields a cursor over a command's rows builds its records over.</summary>
    /// <param name="command">The command the cursor reads.</param>
    /// <param name="container">The container the command is sent to.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The fields, in the order the records hold them, or why the columns of a query that joins or stacks containers cannot be read.</returns>
    public static IGenericResult<IReadOnlyList<IDataField>> Of(IDataCommand command, IDataContainer container, ILogger logger)
    {
        if (command is not IQueryCommand query || (query is not { Joins.Count: > 0 } && query.Unions is not { Count: > 0 }))
            return GenericResult<IReadOnlyList<IDataField>>.Success([.. container.Schema.Fields.OfType<IDataField>()]);

        var columns = query.Projection?.Fields ?? [];
        if (columns.Count == 0)
            return GenericResult<IReadOnlyList<IDataField>>.Failure(CursorFieldsLog.CombinedColumnsNotStated(logger, container.Name));

        if (columns.FirstOrDefault(column => string.IsNullOrWhiteSpace(column.Alias)) is { } unaliased)
            return GenericResult<IReadOnlyList<IDataField>>.Failure(CursorFieldsLog.CombinedColumnWithoutAlias(logger, container.Name, unaliased.PropertyName));

        return GenericResult<IReadOnlyList<IDataField>>.Success([.. columns.Select(column => column.Alias).OfType<string>().Select((alias, ordinal) => new RecordField(alias, ordinal))]);
    }
}
