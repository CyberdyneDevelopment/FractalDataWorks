using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL decimal type, written as DECIMALNTYPE: a sign byte and the magnitude at the column's scale.
/// </summary>
/// <remarks>
/// A value is written exactly or not at all. One with more fractional digits than the column's scale, or
/// more digits than its precision, is refused: rounding it would write a number the source does not hold.
/// </remarks>
[TypeOption(typeof(SqlDataTypes), "Decimal", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlDecimalType : SqlDataTypeBase
{
    /// <summary>The largest precision SQL Server declares.</summary>
    public const byte MaxPrecision = 38;

    /// <summary>Initializes a new instance of the <see cref="SqlDecimalType"/> class.</summary>
    public SqlDecimalType()
        : base(name: "Decimal", sqlName: "decimal", systemTypeId: 106, hasLength: false, hasPrecision: true, hasScale: false)
    {
    }

    /// <summary>Gets how many bytes a decimal of the given precision takes, sign byte included.</summary>
    /// <param name="precision">The precision, 1 to 38.</param>
    /// <returns>5, 9, 13 or 17.</returns>
    public static byte StorageLength(byte precision) => precision <= 9 ? (byte)5 : precision <= 19 ? (byte)9 : precision <= 28 ? (byte)13 : (byte)17;

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(decimal);

    /// <inheritdoc/>
    public override string Declaration(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return string.Create(CultureInfo.InvariantCulture, $"decimal({column.Precision},{column.Scale})");
    }

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return StorageLength(column.Precision);
    }

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(column);
        writer.WriteByte(TdsTypeIds.DecimalN);
        writer.WriteByte(StorageLength(column.Precision));
        writer.WriteByte(column.Precision);
        writer.WriteByte(column.Scale);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        if (!TryConvert<decimal>(value, out var number) || number.Scale > column.Scale)
            return false;

        var magnitude = Magnitude(number, column.Scale);
        if (magnitude.ToString(CultureInfo.InvariantCulture).Length > column.Precision)
            return false;

        var length = StorageLength(column.Precision);
        var bytes = magnitude.ToByteArray(isUnsigned: true, isBigEndian: false);
        if (bytes.Length > length - 1)
            return false;

        writer.WriteByte(length);
        writer.WriteByte(number < 0 ? (byte)0 : (byte)1);
        writer.WriteBytes(bytes);
        writer.WriteBytes(new byte[length - 1 - bytes.Length]);
        return true;
    }

    /// <summary>Gets a decimal's digits as a whole number at the given scale, without its sign.</summary>
    /// <param name="number">The number.</param>
    /// <param name="scale">The scale to express it at; at least the number's own.</param>
    /// <returns>|number| × 10^scale.</returns>
    public static BigInteger Magnitude(decimal number, byte scale)
    {
        var bits = decimal.GetBits(number);
        var unscaled = new BigInteger((uint)bits[0])
            | (new BigInteger((uint)bits[1]) << 32)
            | (new BigInteger((uint)bits[2]) << 64);
        return unscaled * BigInteger.Pow(10, scale - number.Scale);
    }
}
