using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// A schema path (database schema) containing containers.
/// </summary>
public sealed class SchemaPathDto
{
    /// <summary>Gets or sets the path name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path value.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the containers in this path.</summary>
    public IReadOnlyList<SchemaContainerDto> Containers { get; set; } = [];

    /// <summary>Maps from configuration to DTO.</summary>
    public static SchemaPathDto FromConfig(DataPathConfiguration config)
    {
        return new SchemaPathDto
        {
            Name = config.Name,
            Path = config.Path,
            Containers = (config.Containers ?? []).Select(c => SchemaContainerDto.FromConfig(c)).ToList()
        };
    }
}
