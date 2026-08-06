namespace Fdw.Data.Abstractions;

/// <summary>
/// The view of a data type that PostgreSQL's vocabulary uses: sized character and binary types, scaled
/// numerics, and no unicode distinction.
/// </summary>
/// <remarks>
/// Why <see cref="IMsSqlDataType.IsUnicode"/> has no counterpart here: PostgreSQL encodes the whole
/// database, so there is no per-type unicode choice to make — <c>text</c> and <c>varchar</c> differ in
/// length bounds, not in character handling. Modelling it as a separate vocabulary rather than reusing the
/// SQL Server interface is what keeps that difference visible instead of implied.
/// </remarks>
public interface IPostgreSqlDataType : IGenericDataType
{
    /// <summary>Gets the largest length the type accepts, or null when length does not apply.</summary>
    int? MaxLength { get; }

    /// <summary>Gets the largest precision the type accepts, or null when precision does not apply.</summary>
    int? MaxPrecision { get; }

    /// <summary>Gets the largest scale the type accepts, or null when scale does not apply.</summary>
    int? MaxScale { get; }

    /// <summary>Gets the length applied when a field of this type declares none.</summary>
    int? DefaultLength { get; }

    /// <summary>Gets the precision applied when a field of this type declares none.</summary>
    int? DefaultPrecision { get; }

    /// <summary>Gets the scale applied when a field of this type declares none.</summary>
    int? DefaultScale { get; }

    /// <summary>Gets a value indicating whether a field of this type is meaningless without an explicit length.</summary>
    bool RequiresLength { get; }

    /// <summary>Gets a value indicating whether a field of this type is meaningless without an explicit precision.</summary>
    bool RequiresPrecision { get; }

    /// <summary>Gets a value indicating whether the type is variable-length rather than blank-padded.</summary>
    bool IsVariableLength { get; }

    /// <summary>Gets a value indicating whether the type holds bytes with no text interpretation.</summary>
    bool IsBinary { get; }

    /// <summary>Gets a value indicating whether values can be read without materializing the whole value.</summary>
    bool SupportsStreaming { get; }

    /// <summary>Gets a value indicating whether PostgreSQL has superseded this type.</summary>
    bool IsDeprecated { get; }

    /// <summary>Gets the literal token to emit in DDL.</summary>
    string NativeName { get; }
}
