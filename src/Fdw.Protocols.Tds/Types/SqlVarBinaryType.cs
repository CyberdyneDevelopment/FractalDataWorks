using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL varbinary type: bytes as they are.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "VarBinary", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlVarBinaryType : SqlVariableLengthTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlVarBinaryType"/> class.</summary>
    public SqlVarBinaryType()
        : base(name: "VarBinary", sqlName: "varbinary", systemTypeId: 165, typeId: TdsTypeIds.BigVarBinary, collated: false, bytesPerUnit: 1)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(byte[]);

    /// <inheritdoc/>
    protected override bool TryEncode(object value, out byte[] bytes)
    {
        if (value is byte[] raw)
        {
            bytes = raw;
            return true;
        }

        bytes = [];
        return false;
    }
}
