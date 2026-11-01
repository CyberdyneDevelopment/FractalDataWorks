using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// A SQL Server-specific data field that extends <see cref="IDataField"/> with native type
/// metadata and storage-specific modifiers.
/// </summary>
/// <remarks>
/// Consumers working at the MsSql layer cast <see cref="IDataField"/> to
/// <see cref="IMsSqlDataField"/> to access the native type and precision/scale metadata
/// directly — no dictionary lookup required.
/// </remarks>
public interface IMsSqlDataField : IDataField
{
    /// <summary>
    /// Gets the SQL Server native type for this field (e.g., <c>bigint</c>, <c>nvarchar</c>).
    /// </summary>
    DataTypeOptionBase NativeType { get; }

    /// <summary>
    /// Gets the numeric precision of this field, if applicable.
    /// </summary>
    /// <remarks>
    /// Populated for <c>decimal</c>, <c>numeric</c>, <c>float</c>, and datetime types.
    /// <see langword="null"/> for types where precision is fixed or not applicable.
    /// </remarks>
    int? Precision { get; }

    /// <summary>
    /// Gets the numeric scale of this field, if applicable.
    /// </summary>
    /// <remarks>
    /// Populated for <c>decimal</c>, <c>numeric</c>, and <c>time</c>/<c>datetime2</c> fractional seconds.
    /// <see langword="null"/> for types where scale is not applicable.
    /// </remarks>
    int? Scale { get; }

    /// <summary>
    /// Gets the maximum length in bytes (for binary types) or characters (for character types).
    /// </summary>
    /// <remarks>
    /// Populated for <c>varchar</c>, <c>nvarchar</c>, <c>char</c>, <c>nchar</c>,
    /// <c>varbinary</c>, and <c>binary</c>. -1 indicates <c>MAX</c>.
    /// <see langword="null"/> for fixed-length types.
    /// </remarks>
    int? MaxLength { get; }

    /// <summary>
    /// Gets the collation name for character-type fields, if explicitly set.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the field inherits the database default collation.
    /// </remarks>
    string? Collation { get; }
}
