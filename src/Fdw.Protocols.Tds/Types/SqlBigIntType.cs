using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL bigint type: a 64-bit signed integer.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "BigInt", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlBigIntType : SqlIntegerTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlBigIntType"/> class.</summary>
    public SqlBigIntType()
        : base(name: "BigInt", sqlName: "bigint", systemTypeId: 127, width: 8, clrType: typeof(long))
    {
    }

    /// <inheritdoc/>
    protected override bool TryWiden(object value, out long whole)
    {
        var converted = TryConvert<long>(value, out var exact);
        whole = exact;
        return converted;
    }
}
