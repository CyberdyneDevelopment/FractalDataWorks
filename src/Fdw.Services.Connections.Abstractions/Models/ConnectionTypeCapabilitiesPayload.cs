using System.Collections.Generic;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Describes the runtime capabilities declared by a specific connection type.
/// </summary>
public sealed class ConnectionTypeCapabilitiesPayload
{
    /// <summary>Gets or sets the container kinds supported (e.g. "Table", "View").</summary>
    public IReadOnlyList<string> ContainerTypes { get; set; } = [];

    /// <summary>Gets or sets the container kinds the Execute command can run against; empty when Execute is unsupported.</summary>
    public IReadOnlyList<string> ExecutableContainerTypes { get; set; } = [];

    /// <summary>Gets or sets the field type descriptors supported by this connection type.</summary>
    public IReadOnlyList<FieldTypeInfoPayload> FieldTypes { get; set; } = [];

    /// <summary>Gets or sets the write strategies supported (e.g. "Append", "Upsert").</summary>
    public IReadOnlyList<string> WriteModes { get; set; } = [];

    /// <summary>Gets or sets the path format templates supported (e.g. "{schema}.{table}").</summary>
    public IReadOnlyList<string> PathFormats { get; set; } = [];

    /// <summary>Gets or sets the schema discovery options the connection type offers; only <c>None</c> when it cannot discover.</summary>
    public IReadOnlyList<SchemaDiscoveryTypeInfo> DiscoveryTypes { get; set; } = [];
}
