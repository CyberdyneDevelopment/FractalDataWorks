using System.Collections.Generic;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// A SQL dialect that can take a key list to a table instead of to parameters (FDW-4267).
/// </summary>
/// <remarks>
/// <para>
/// Each dialect has its own form, because each engine has its own way to hold a list of values the statement joins on: SQL Server a temporary table filled from
/// one JSON parameter; PostgreSQL and SQLite a subquery over one JSON parameter. A dialect that does not implement this keeps every key list as parameters.
/// </para>
/// <para>
/// The list travels as one text parameter whatever its length, so the statement carries one parameter per list and not one per value.
/// </para>
/// </remarks>
public interface IKeyListSpillDialect
{
    /// <summary>Gets the SQL that holds a key list in a table and the subquery that reads it back.</summary>
    /// <param name="uniqueId">A name unique to this list within the statement and across connections, safe in an identifier (letters and digits).</param>
    /// <param name="sourceTable">The quoted, qualified table the filtered column belongs to; the key table is typed like its column.</param>
    /// <param name="columnName">The unquoted name of the filtered column.</param>
    /// <param name="jsonParameter">The SQL text of the parameter that carries the list as JSON (prefix included).</param>
    /// <returns>The statements before and after the main statement, and the subquery that replaces the parameter list.</returns>
    KeyListSpillForm KeyListSpill(string uniqueId, string sourceTable, string columnName, string jsonParameter);

    /// <summary>Gets the JSON the key list travels as, in the shape this dialect's form reads.</summary>
    /// <param name="columnName">The unquoted name of the filtered column.</param>
    /// <param name="values">The values of the list.</param>
    /// <returns>The JSON text.</returns>
    string KeyListJson(string columnName, IReadOnlyList<object?> values);
}
