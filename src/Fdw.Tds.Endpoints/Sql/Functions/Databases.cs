using System;
using Fdw.Tds.Endpoints.Hosting;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// The databases a caller can name: master, which every SQL Server has and holds no tables here, and the
/// one database this server serves.
/// </summary>
public static class Databases
{
    /// <summary>The name of the master database.</summary>
    public const string Master = "master";

    /// <summary>The id of the master database.</summary>
    public const int MasterId = 1;

    /// <summary>Gets the name a database id names.</summary>
    /// <param name="id">The id; null for NULL.</param>
    /// <param name="context">The request.</param>
    /// <returns>The name, or null for an id no database has.</returns>
    public static string? NameOf(long? id, SqlExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (id == TdsServerIdentity.DatabaseId)
            return context.Server.DatabaseName;

        return id == MasterId ? Master : null;
    }

    /// <summary>Gets the id a database name names.</summary>
    /// <param name="name">The name; null for NULL.</param>
    /// <param name="context">The request.</param>
    /// <returns>The id, or null for a name no database has.</returns>
    public static int? IdOf(string? name, SqlExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.Equals(name, context.Server.DatabaseName, StringComparison.OrdinalIgnoreCase))
            return TdsServerIdentity.DatabaseId;

        return string.Equals(name, Master, StringComparison.OrdinalIgnoreCase) ? MasterId : null;
    }

    /// <summary>Gets a value indicating whether a name, as written in a three-part object name, is one a catalog view answers in.</summary>
    /// <param name="name">The database part written, or null when none was.</param>
    /// <param name="context">The request.</param>
    /// <returns>True for no database part, master, or the database this server serves.</returns>
    public static bool IsKnown(string? name, SqlExecutionContext context)
        => name is null || IdOf(name, context) is not null;
}
