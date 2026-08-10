using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Data;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// DTO representing the discovered schema for a connection.
/// </summary>
public sealed class SchemaInformationDto
{
    /// <summary>Gets or sets the DataStore name.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection ID this DataStore was discovered from.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets when schema was last discovered.</summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }

    /// <summary>Gets or sets the discovered paths (schemas) with their containers and fields.</summary>
    public IReadOnlyList<SchemaPathDto> Paths { get; set; } = [];

    /// <summary>Maps from the domain model to this DTO.</summary>
    public static SchemaInformationDto FromSchema(SchemaInformation schema)
    {
        return new SchemaInformationDto
        {
            DataStoreName = schema.DataStore.Name,
            ConnectionId = schema.ConnectionId,
            LastDiscoveredAt = schema.LastDiscoveredAt,
            Paths = schema.Paths.Select(p => SchemaPathDto.FromConfig(p)).ToList()
        };
    }
}
