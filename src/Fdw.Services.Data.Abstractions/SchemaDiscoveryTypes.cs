using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Registry of schema discovery types. Each type represents a store-specific
/// schema discoverer (MsSql, PostgreSql, etc.).
/// </summary>
/// <remarks>
/// Uses [MutableTypeCollection] to support cross-assembly TypeOption registration
/// (e.g., MsSqlSchemaDiscoveryType in Services.Connections.MsSql).
/// Registration and initialization are orchestrated by ConfigurationGatewayDataStoreProvider during
/// three-phase DI registration.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(
    typeof(SchemaDiscoveryTypeBase),
    typeof(ISchemaDiscoveryType),
    typeof(SchemaDiscoveryTypes))]
public abstract partial class SchemaDiscoveryTypes : TypeCollectionBase<
    SchemaDiscoveryTypeBase,
    ISchemaDiscoveryType>
{
    /// <summary>
    /// The service category for schema discovery types.
    /// </summary>
    public static string ServiceCategory => "SchemaDiscovery";
}
