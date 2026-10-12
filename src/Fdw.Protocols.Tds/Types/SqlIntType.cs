using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL int type: a 32-bit signed integer.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "Int", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlIntType : SqlIntegerTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlIntType"/> class.</summary>
    public SqlIntType()
        : base(name: "Int", sqlName: "int", systemTypeId: 56, width: 4, clrType: typeof(int))
    {
    }

    /// <inheritdoc/>
    protected override bool TryWiden(object value, out long whole)
    {
        var converted = TryConvert<int>(value, out var exact);
        whole = exact;
        return converted;
    }
}
