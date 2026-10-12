using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL tinyint type: an 8-bit unsigned integer.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "TinyInt", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlTinyIntType : SqlIntegerTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlTinyIntType"/> class.</summary>
    public SqlTinyIntType()
        : base(name: "TinyInt", sqlName: "tinyint", systemTypeId: 48, width: 1, clrType: typeof(byte))
    {
    }

    /// <inheritdoc/>
    protected override bool TryWiden(object value, out long whole)
    {
        var converted = TryConvert<byte>(value, out var exact);
        whole = exact;
        return converted;
    }
}
