using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>numeric</c> — normalizes to <see cref="DataTypes.Decimal"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "numeric")]
public sealed class NumericType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="NumericType"/> class.</summary>
    public NumericType()
        : base(
            name: "numeric",
            description: "Fixed-precision decimal number; the same type as decimal.",
            abstractType: DataTypes.Decimal,
            isNumeric: true, maxPrecision: 38, maxScale: 38, defaultPrecision: 18, defaultScale: 0, requiresPrecision: true)
    {
    }
}
