using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL float type: a 64-bit floating-point number.
/// </summary>
[TypeOption(typeof(SqlDataTypes), "Float", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlFloatType : SqlFloatTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlFloatType"/> class.</summary>
    public SqlFloatType()
        : base(name: "Float", sqlName: "float", systemTypeId: 62, width: 8, clrType: typeof(double))
    {
    }

    /// <inheritdoc/>
    protected override bool TryWriteNumber(TdsPayloadWriter writer, object value)
    {
        if (!TryConvert<double>(value, out var number))
            return false;

        writer.WriteByte(8);
        writer.WriteDouble(number);
        return true;
    }
}
