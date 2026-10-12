using System;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// A SQL type with the facets one column declares: a length, or a precision and scale.
/// </summary>
public sealed class TdsColumnType
{
    /// <summary>The <see cref="Length"/> of a (max) type.</summary>
    public const int Max = -1;

    /// <summary>Initializes a new instance of the <see cref="TdsColumnType"/> class.</summary>
    /// <param name="dataType">The SQL type.</param>
    /// <param name="length">Characters for a character type, bytes for a binary one, or <see cref="Max"/>; zero for a type without a length.</param>
    /// <param name="precision">The precision of a decimal; zero for any other type.</param>
    /// <param name="scale">The scale of a decimal, or the fractional-second digits of a time type.</param>
    public TdsColumnType(ISqlDataType dataType, int length, byte precision, byte scale)
    {
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Length = length;
        Precision = precision;
        Scale = scale;
    }

    /// <summary>Gets the SQL type.</summary>
    public ISqlDataType DataType { get; }

    /// <summary>Gets the length: characters for a character type, bytes for a binary one, or <see cref="Max"/>.</summary>
    public int Length { get; }

    /// <summary>Gets the precision of a decimal.</summary>
    public byte Precision { get; }

    /// <summary>Gets the scale of a decimal, or the fractional-second digits of a time type.</summary>
    public byte Scale { get; }

    /// <summary>Gets a value indicating whether the column is a (max) type.</summary>
    public bool IsMax => Length == Max;

    /// <summary>Gets the type as the column declares it.</summary>
    /// <returns>The declaration, such as nvarchar(50).</returns>
    public override string ToString() => DataType.Declaration(this);
}
