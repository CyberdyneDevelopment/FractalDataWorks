using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL real type: a 32-bit floating-point number.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "Real", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlRealType : SqlFloatTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlRealType"/> class.</summary>
    public SqlRealType()
        : base(name: "Real", sqlName: "real", systemTypeId: 59, width: 4, clrType: typeof(float))
    {
    }

    /// <inheritdoc/>
    protected override bool TryWriteNumber(TdsPayloadWriter writer, object value)
    {
        if (!TryConvert<float>(value, out var number))
            return false;

        writer.WriteByte(4);
        writer.WriteSingle(number);
        return true;
    }
}
