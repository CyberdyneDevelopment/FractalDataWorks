using Fdw.Services.Connections;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// A schema field (column).
/// </summary>
public sealed class SchemaFieldDto
{
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field is nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }

    /// <summary>Maps from configuration to DTO.</summary>
    public static SchemaFieldDto FromConfig(DataContainerFieldConfiguration config)
    {
        return new SchemaFieldDto
        {
            Name = config.Name,
            DataType = config.DataType ?? string.Empty,
            IsNullable = false,
            Ordinal = 0
        };
    }
}
