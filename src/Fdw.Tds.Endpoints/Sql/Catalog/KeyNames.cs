using System;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// The name the catalog gives a table's primary key, which its data set declares only as key fields.
/// </summary>
public static class KeyNames
{
    /// <summary>Gets the primary key's name: PK_ and the table's name, as SQL Server names one it generates.</summary>
    /// <param name="table">The table.</param>
    /// <returns>The key's name.</returns>
    public static string Of(SqlTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        return "PK_" + table.Name;
    }
}
