namespace Fdw.Data.Abstractions;

/// <summary>
/// The view of a data type that SQL Server's vocabulary uses: sized character and binary types, and
/// scaled numerics.
/// </summary>
/// <remarks>
/// Why <c>Format</c> is absent: SQL Server types have no wire-format qualifier — a <c>datetime2</c> is a
/// <c>datetime2</c>, not a string with a format. Closing <c>MsSqlNativeTypes</c> on this interface is what
/// makes that unreachable rather than merely undocumented.
/// </remarks>
public interface IMsSqlDataType : IGenericDataType
{
    /// <summary>Gets the largest length the type accepts, or null when length does not apply.</summary>
    /// <remarks>Counted in characters when <see cref="IsUnicode"/>, otherwise in bytes.</remarks>
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

    /// <summary>Gets a value indicating whether the type stores characters rather than bytes.</summary>
    bool IsUnicode { get; }

    /// <summary>Gets a value indicating whether the type is variable-length rather than blank-padded.</summary>
    bool IsVariableLength { get; }

    /// <summary>Gets a value indicating whether the type holds bytes with no text interpretation.</summary>
    bool IsBinary { get; }

    /// <summary>Gets a value indicating whether values can be read without materializing the whole value.</summary>
    bool SupportsStreaming { get; }

    /// <summary>Gets a value indicating whether SQL Server has superseded this type.</summary>
    bool IsDeprecated { get; }

    /// <summary>Gets the literal token to emit in DDL.</summary>
    string NativeName { get; }
}
