using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data;

/// <summary>
/// Declares the child container (table) name for a property-collection (KVP) child whose table
/// is NOT derivable from the property/owner type. The configuration read cascade queries that
/// container for the KVP rows; without this, a KVP property bag cannot be resolved by the mapper
/// alone (a property named <c>Properties</c> maps to <c>conn.MsSqlConnectionAuthentication</c> —
/// no derivable convention), so the cascade would silently load nothing.
/// </summary>
/// <remarks>
/// Applies ONLY to <c>IDictionary&lt;string,string?&gt;</c> property-collection children. Typed-list
/// children resolve their container at runtime via the child type's <c>ConfigurationCommand</c> and
/// need no attribute. The foreign-key column is the standard physical <c>{Owner}RowId</c> convention
/// and is supplied by the generator, not here.
/// </remarks>
// Why: pure attribute definition (declarative metadata only, consumed by the configuration read cascade) — no logic to unit test.
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationChildTableAttribute : Attribute
{
    /// <summary>Initializes the attribute with the child container (table) name.</summary>
    /// <param name="containerName">The child container/table name, e.g. <c>MsSqlConnectionAuthentication</c>.</param>
    public ConfigurationChildTableAttribute(string containerName)
    {
        ContainerName = containerName;
    }

    /// <summary>The child container (table) name that holds this property-collection's rows.</summary>
    public string ContainerName { get; }
}
