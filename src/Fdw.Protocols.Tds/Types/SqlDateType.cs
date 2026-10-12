using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL date type: a calendar date.
/// </summary>
/// <remarks>
/// It holds a <see cref="DateOnly"/>. A <see cref="DateTime"/> is written as datetime2, because dropping
/// its time of day would lose part of the value.
/// </remarks>
[TypeOption(typeof(SqlDataTypes), "Date", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlDateType : SqlDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlDateType"/> class.</summary>
    public SqlDateType()
        : base(name: "Date", sqlName: "date", systemTypeId: 40, hasLength: false, hasPrecision: false, hasScale: false)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(DateOnly);

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column) => 3;

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTypeIds.DateN);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        if (value is not DateOnly date)
            return false;

        writer.WriteByte(3);
        TdsTimeEncoding.WriteDate(writer, date.ToDateTime(TimeOnly.MinValue));
        return true;
    }
}
