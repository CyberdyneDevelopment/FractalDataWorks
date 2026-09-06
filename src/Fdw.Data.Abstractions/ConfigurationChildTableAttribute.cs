using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data;

/// <summary>
/// Declares the child container (table) name for a cascade child whose container is NOT derivable
/// from the property/owner type: a property-collection (KVP) child (a property named
/// <c>Properties</c> maps to <c>conn.MsSqlConnectionAuthentication</c> — no derivable convention),
/// or a typed-list child whose container is not simply the child type name minus the
/// <c>Configuration</c> suffix (<c>IList&lt;DataFieldConfiguration&gt; Fields</c> whose rows live in
/// <c>data.DataSetField</c>). Without it the cascade queries a container that does not exist and
/// silently loads nothing.
/// </summary>
/// <remarks>
/// A typed-list child with no attribute defaults to the child type name minus the
/// <c>Configuration</c> suffix — <c>EscalationLevelConfiguration</c> rows live in
/// <c>EscalationLevel</c> — so most typed-list children need nothing; add this only when the
/// container is named otherwise. The foreign-key column is the standard physical
/// <c>{Owner}RowId</c> convention and is supplied by the generator, not here.
/// </remarks>
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
