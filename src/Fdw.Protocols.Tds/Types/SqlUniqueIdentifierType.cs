using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL uniqueidentifier type: a GUID in the byte order SQL Server and .NET share.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "UniqueIdentifier", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlUniqueIdentifierType : SqlDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlUniqueIdentifierType"/> class.</summary>
    public SqlUniqueIdentifierType()
        : base(name: "UniqueIdentifier", sqlName: "uniqueidentifier", systemTypeId: 36, hasLength: false, hasPrecision: false, hasScale: false)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(Guid);

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column) => 16;

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTypeIds.UniqueIdentifier);
        writer.WriteByte(16);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        Guid? id = value switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };

        if (id is not { } written)
            return false;

        writer.WriteByte(16);
        writer.WriteBytes(written.ToByteArray());
        return true;
    }
}
