using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.ValueSources;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Checks the column names a load command carries against the target container's schema: every name must be safe
/// to quote and must be a column of the container.
/// </summary>
internal static class MsSqlLoadColumns
{
    /// <summary>Finds the container's field named <paramref name="name"/> (case-insensitive), or <see langword="null"/>.</summary>
    internal static IField? Find(IStorageContainer container, string name) =>
        container.Schema.Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Describes the first name that is unsafe or not a column of the container, or returns <see langword="null"/>.</summary>
    internal static string? UnknownOrUnsafe(IStorageContainer container, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!MsSqlIdentifiers.IsSafe(name))
                return $"'{name}' is not a safe column name (letters, digits and underscores only)";
            if (Find(container, name) is null)
                return $"column '{name}' does not exist in '{container.Name}'";
        }

        return null;
    }

    /// <summary>Returns whether the container's column <paramref name="name"/> is an identity column.</summary>
    internal static bool IsIdentity(IStorageContainer container, string name) => Find(container, name)?.IsIdentity == true;

    /// <summary>Returns whether the container's column <paramref name="name"/> is computed.</summary>
    internal static bool IsComputed(IStorageContainer container, string name) => Find(container, name)?.IsComputed == true;
}
