using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL smallint type: a 16-bit signed integer.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "SmallInt", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlSmallIntType : SqlIntegerTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlSmallIntType"/> class.</summary>
    public SqlSmallIntType()
        : base(name: "SmallInt", sqlName: "smallint", systemTypeId: 52, width: 2, clrType: typeof(short))
    {
    }

    /// <inheritdoc/>
    protected override bool TryWiden(object value, out long whole)
    {
        var converted = TryConvert<short>(value, out var exact);
        whole = exact;
        return converted;
    }
}
