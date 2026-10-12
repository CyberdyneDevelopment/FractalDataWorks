using System;
using System.Collections.Generic;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// The permissions a caller of this endpoint holds: those that read. Nothing that changes data or
/// definitions is ever granted, because this server only reads.
/// </summary>
public static class ReadPermissions
{
    private static readonly HashSet<string> Held = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT",
        "CONNECT",
        "VIEW DEFINITION",
        "VIEW ANY DEFINITION",
        "REFERENCES",
    };

    /// <summary>Gets a value indicating whether the caller holds a permission.</summary>
    /// <param name="permission">The permission's name, such as SELECT.</param>
    /// <returns>True for a permission that reads.</returns>
    public static bool Holds(string permission) => Held.Contains(permission.Trim());
}
