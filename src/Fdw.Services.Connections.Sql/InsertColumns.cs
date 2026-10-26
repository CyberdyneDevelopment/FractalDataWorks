using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// Chooses the columns an insert writes, the same way on every route: the serial batched statements and a connection's bulk copy.
/// </summary>
/// <remarks>
/// A column is written when at least one row supplies it (an entity has a property of that name, a dictionary row has that key).
/// A column no row supplies is left out of the statement, so the database default applies, as it does when one row is inserted alone.
/// A row that lacks a column other rows supply binds NULL for it.
/// </remarks>
public static class InsertColumns
{
    /// <summary>The insertable fields that at least one row supplies.</summary>
    /// <param name="insertable">The container's insertable fields, in column order.</param>
    /// <param name="rows">The rows to insert.</param>
    /// <returns>The fields to write, in column order.</returns>
    public static List<IField> Supplied(IReadOnlyList<IField> insertable, IReadOnlyList<object> rows)
        => insertable.Where(field => rows.Any(row => RowValues.TryRead(row, field.Name, out _, out _))).ToList();
}
