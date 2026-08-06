using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// A PostgreSQL-specific data field that extends <see cref="IDataField"/> with native type
/// metadata and storage-specific modifiers.
/// </summary>
/// <remarks>
/// Consumers working at the PostgreSQL layer cast <see cref="IDataField"/> to
/// <see cref="IPostgreSqlDataField"/> to access the native type and precision/scale metadata
/// directly — no dictionary lookup required.
/// </remarks>
public interface IPostgreSqlDataField : IDataField
{
    /// <summary>
    /// Gets the PostgreSQL native type for this field (e.g., <c>int8</c>, <c>text</c>).
    /// </summary>
    PostgreSqlNativeTypeBase NativeType { get; }

    /// <summary>
    /// Gets the numeric precision of this field, if applicable.
    /// </summary>
    /// <remarks>
    /// Populated for <c>numeric</c> and floating-point types.
    /// <see langword="null"/> for types where precision is fixed or not applicable.
    /// </remarks>
    int? Precision { get; }

    /// <summary>
    /// Gets the numeric scale of this field, if applicable.
    /// </summary>
    /// <remarks>
    /// Populated for <c>numeric</c> and interval fractional seconds.
    /// <see langword="null"/> for types where scale is not applicable.
    /// </remarks>
    int? Scale { get; }

    /// <summary>
    /// Gets the maximum length in characters for character-type fields.
    /// </summary>
    /// <remarks>
    /// Populated for <c>varchar</c> and <c>char</c> with explicit length constraints.
    /// <see langword="null"/> for unbounded types such as <c>text</c>.
    /// </remarks>
    int? MaxLength { get; }
}
