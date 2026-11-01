namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>
/// Represents a discovered column.
/// </summary>
public sealed class DiscoveredField
{
    /// <summary>
    /// Gets the column name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the SQL data type (e.g., "int", "nvarchar", "datetime2").
    /// </summary>
    public required string SqlType { get; init; }

    /// <summary>
    /// Gets whether the column is nullable.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// Gets the ordinal position (1-based).
    /// </summary>
    public required int Ordinal { get; init; }

    /// <summary>
    /// Gets the maximum length for string/binary types.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets the numeric precision.
    /// </summary>
    public int? Precision { get; init; }

    /// <summary>
    /// Gets the numeric scale.
    /// </summary>
    public int? Scale { get; init; }

    /// <summary>
    /// Gets whether this column is part of the primary key.
    /// </summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>
    /// Gets whether this column is an identity/auto-increment column.
    /// </summary>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Gets whether this column is computed.
    /// </summary>
    public bool IsComputed { get; init; }

    /// <summary>
    /// Gets the default value expression.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets the column description from MS_Description extended property.
    /// </summary>
    public string? Description { get; init; }
}
