using System;
using System.Collections.Generic;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// What DATABASEPROPERTYEX answers for the one database this server holds.
/// </summary>
/// <remarks>
/// The database is online, read-only and open to every user, because that is what this server serves. A
/// property name SQL Server does not define is NULL.
/// </remarks>
public static class DatabaseProperties
{
    private static readonly Dictionary<string, Func<SqlExecutionContext, object?>> Properties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Collation"] = static c => c.Server.Collation.Name,
        ["Status"] = static _ => "ONLINE",
        ["Updateability"] = static _ => "READ_ONLY",
        ["UserAccess"] = static _ => "MULTI_USER",
        ["Recovery"] = static _ => "SIMPLE",
        ["Version"] = static _ => 957,
        ["LCID"] = static c => ServerProperties.Lcid(c),
        ["SQLSortOrder"] = static c => (int)c.Server.Collation.Bytes[4],
        ["ComparisonStyle"] = static _ => 196609,
        ["IsAutoClose"] = static _ => 0,
        ["IsAutoShrink"] = static _ => 0,
        ["IsInStandBy"] = static _ => 0,
        ["IsFulltextEnabled"] = static _ => 0,
        ["IsAnsiNullsEnabled"] = static _ => 1,
        ["IsQuotedIdentifiersEnabled"] = static _ => 1,
    };

    /// <summary>Reads a property.</summary>
    /// <param name="name">The property's name.</param>
    /// <param name="context">The request.</param>
    /// <param name="value">The value; null for NULL.</param>
    /// <returns>False for a name SQL Server does not define.</returns>
    public static bool TryRead(string name, SqlExecutionContext context, out object? value)
    {
        if (Properties.TryGetValue(name, out var read))
        {
            value = read(context);
            return true;
        }

        value = null;
        return false;
    }
}
