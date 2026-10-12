using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL datetime2 type: a date and time of day with no offset.
/// </summary>
/// <remarks>
/// It holds a <see cref="DateTime"/>. A <see cref="DateTimeOffset"/> is refused rather than stripped of its
/// offset: which clock time it should become is a decision, not a conversion.
/// </remarks>
[TypeOption(typeof(SqlDataTypes), "DateTime2", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlDateTime2Type : SqlDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlDateTime2Type"/> class.</summary>
    public SqlDateTime2Type()
        : base(name: "DateTime2", sqlName: "datetime2", systemTypeId: 42, hasLength: false, hasPrecision: false, hasScale: true)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(DateTime);

    /// <inheritdoc/>
    public override string Declaration(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return string.Create(CultureInfo.InvariantCulture, $"datetime2({column.Scale})");
    }

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return (short)(TdsTimeEncoding.TimeLength(column.Scale) + 3);
    }

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(column);
        writer.WriteByte(TdsTypeIds.DateTime2N);
        writer.WriteByte(column.Scale);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        if (value is not DateTime moment)
            return false;

        writer.WriteByte((byte)(TdsTimeEncoding.TimeLength(column.Scale) + 3));
        TdsTimeEncoding.WriteTime(writer, moment.TimeOfDay, column.Scale);
        TdsTimeEncoding.WriteDate(writer, moment);
        return true;
    }
}
