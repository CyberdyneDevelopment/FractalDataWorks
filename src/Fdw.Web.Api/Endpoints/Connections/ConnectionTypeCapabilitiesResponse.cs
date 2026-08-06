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
}
