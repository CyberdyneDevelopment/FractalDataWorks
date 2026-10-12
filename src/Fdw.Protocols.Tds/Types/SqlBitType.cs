using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL bit type, written as BITNTYPE.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "Bit", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlBitType : SqlDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlBitType"/> class.</summary>
    public SqlBitType()
        : base(name: "Bit", sqlName: "bit", systemTypeId: 104, hasLength: false, hasPrecision: false, hasScale: false)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(bool);

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column) => 1;

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTypeIds.BitN);
        writer.WriteByte(1);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        if (!TryConvert<bool>(value, out var flag))
            return false;

        writer.WriteByte(1);
        writer.WriteByte(flag ? (byte)1 : (byte)0);
        return true;
    }
}
