using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The ways a connection can discover the schema of the store it opens: one option per strategy
/// (a SQL Server catalog, a PostgreSQL catalog, ...) plus <see cref="NoneSchemaDiscovery"/> for a
/// connection that does not discover.
/// </summary>
/// <remarks>
/// <para>
/// A connection type declares the options it offers
/// (<see cref="IConnectionType.SupportedDiscoveryTypes"/>) and a connection's configuration names the
/// one it uses (<see cref="IConnectionImplementationConfiguration.SchemaDiscovery"/>). Both are read
/// off members of this collection; there is no separate "supports discovery" flag to disagree with them.
/// </para>
/// <para>
/// Why here, beside <see cref="IConnectionType"/> rather than in the data abstractions: the connection
/// type declares these the same way it declares its commands and session contexts, and
/// <c>Fdw.Services.Data.Abstractions</c> already depends on this package, not the other way round.
/// </para>
/// <para>
/// Options register from any loaded assembly with
/// <c>[TypeOption(typeof(SchemaDiscoveryTypes), "Name")]</c>. The SQL catalog options live with their
/// connection types in the reference connection packages.
/// </para>
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
