using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL datetimeoffset type: a UTC date and time of day, and the offset it was recorded at.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "DateTimeOffset", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlDateTimeOffsetType : SqlDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlDateTimeOffsetType"/> class.</summary>
    public SqlDateTimeOffsetType()
        : base(name: "DateTimeOffset", sqlName: "datetimeoffset", systemTypeId: 43, hasLength: false, hasPrecision: false, hasScale: true)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(DateTimeOffset);

    /// <inheritdoc/>
    public override string Declaration(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return string.Create(CultureInfo.InvariantCulture, $"datetimeoffset({column.Scale})");
    }

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return (short)(TdsTimeEncoding.TimeLength(column.Scale) + 5);
    }

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(column);
        writer.WriteByte(TdsTypeIds.DateTimeOffsetN);
        writer.WriteByte(column.Scale);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        if (value is not DateTimeOffset moment)
            return false;

        var utc = moment.UtcDateTime;
        writer.WriteByte((byte)(TdsTimeEncoding.TimeLength(column.Scale) + 5));
        TdsTimeEncoding.WriteTime(writer, utc.TimeOfDay, column.Scale);
        TdsTimeEncoding.WriteDate(writer, utc);
        writer.WriteInt16((short)moment.Offset.TotalMinutes);
        return true;
    }
}
