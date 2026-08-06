using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>float</c> — normalizes to <see cref="DataTypes.Double"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "Float")]
public sealed class FloatType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatType"/> class.</summary>
    public FloatType()
        : base(
            id: 16,
            name: "Float",
            description: "Double-precision floating-point number.",
            abstractType: DataTypes.Double,
            isNumeric: true, maxPrecision: 53, defaultPrecision: 53, nativeName: "float")
    {
    }
}
