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
    /// <remarks>
    /// Named PathName and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this string and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the containers in this path.</summary>
    public IReadOnlyList<SchemaContainerDto> Containers { get; set; } = [];

    /// <summary>Maps from configuration to DTO.</summary>
    public static SchemaPathDto FromConfig(DataPathConfiguration config)
    {
        return new SchemaPathDto
        {
            Name = config.Name,
            PathName = config.PathName,
            Containers = (config.Containers ?? []).Select(c => SchemaContainerDto.FromConfig(c)).ToList()
        };
    }
}
