using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL time type: a time of day. A span of a day or more, or a negative one, is not a time of day
/// and is refused.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "Time", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlTimeType : SqlDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlTimeType"/> class.</summary>
    public SqlTimeType()
        : base(name: "Time", sqlName: "time", systemTypeId: 41, hasLength: false, hasPrecision: false, hasScale: true)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(TimeSpan) || clrType == typeof(TimeOnly);

    /// <inheritdoc/>
    public override string Declaration(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return string.Create(CultureInfo.InvariantCulture, $"time({column.Scale})");
    }

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return (short)TdsTimeEncoding.TimeLength(column.Scale);
    }

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(column);
        writer.WriteByte(TdsTypeIds.TimeN);
        writer.WriteByte(column.Scale);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        TimeSpan? timeOfDay = value switch
        {
            TimeSpan span when span >= TimeSpan.Zero && span < TimeSpan.FromDays(1) => span,
            TimeOnly time => time.ToTimeSpan(),
            _ => null,
        };

        if (timeOfDay is not { } written)
            return false;

        writer.WriteByte((byte)TdsTimeEncoding.TimeLength(column.Scale));
        TdsTimeEncoding.WriteTime(writer, written, column.Scale);
        return true;
    }
}
