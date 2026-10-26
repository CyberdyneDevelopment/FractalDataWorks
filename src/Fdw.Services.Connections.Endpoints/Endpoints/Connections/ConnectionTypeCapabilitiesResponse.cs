using System.Collections.Generic;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Response DTO describing the runtime capabilities of a connection type.
/// </summary>
public sealed class ConnectionTypeCapabilitiesResponse
{
    /// <summary>Gets or sets the container kinds supported (e.g. "Table", "View").</summary>
    public IReadOnlyList<string> ContainerTypes { get; set; } = [];

    /// <summary>Gets or sets the field type descriptors supported by this connection type.</summary>
    public IReadOnlyList<FieldTypeInfo> FieldTypes { get; set; } = [];

    /// <summary>Gets or sets the write strategies supported (e.g. "Append", "Upsert").</summary>
    public IReadOnlyList<string> WriteModes { get; set; } = [];

    /// <summary>Gets or sets the path format templates supported (e.g. "{schema}.{table}").</summary>
    public IReadOnlyList<string> PathFormats { get; set; } = [];

    /// <summary>Gets or sets the wire protocol names supported (e.g. "Rest", "GraphQL", "Soap11").</summary>
    public IReadOnlyList<string> Protocols { get; set; } = [];

    /// <summary>
    /// Gets or sets the names of the data store types a data store over this kind of connection may have, limited to
    /// the data store types this deployment registered. The first is the default when a create-data-store request
    /// names none (its <c>storeType</c>). Empty when none of the kind's data store types is registered.
    /// </summary>
    public IReadOnlyList<string> DataStoreTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the schema discovery options the connection type offers. A type that cannot
    /// discover offers only <c>None</c>; the list is never empty.
    /// </summary>
    public IReadOnlyList<SchemaDiscoveryTypeInfo> DiscoveryTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the authentication methods a connection of this type may name, each with the credentials it asks
    /// for, so a create form can ask for them before the connection exists (FDW-1090). Empty for a type that signs in
    /// with no method of its own.
    /// </summary>
    public IReadOnlyList<ConnectionAuthenticationTypeInfo> AuthenticationTypes { get; set; } = [];
}
