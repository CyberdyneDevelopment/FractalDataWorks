using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// A schema container (table or view) with its fields.
/// </summary>
public sealed class SchemaContainerDto
{
    /// <summary>Gets or sets the container name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (Table, View).</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of fields.</summary>
    public int FieldCount { get; set; }

    /// <summary>Gets or sets the fields in this container.</summary>
    public IReadOnlyList<SchemaFieldDto> Fields { get; set; } = [];

    /// <summary>Maps from configuration to DTO.</summary>
    public static SchemaContainerDto FromConfig(DataContainerConfiguration config)
    {
        var fields = (config.Fields ?? []).Select(f => SchemaFieldDto.FromConfig(f)).ToList();
        return new SchemaContainerDto
        {
            Name = config.Name,
            // Why: TypeId replaces ContainerType after Wave A5 DDL rename.
            ContainerType = config.TypeId ?? string.Empty,
            FieldCount = fields.Count,
            Fields = fields
        };
    }
}
