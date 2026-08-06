using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>decimal</c> — normalizes to <see cref="DataTypes.Decimal"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "Decimal")]
public sealed class DecimalType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalType"/> class.</summary>
    public DecimalType()
        : base(
            id: 15,
            name: "Decimal",
            description: "Fixed-precision decimal number.",
            abstractType: DataTypes.Decimal,
            isNumeric: true, maxPrecision: 38, maxScale: 38, defaultPrecision: 18, defaultScale: 0, requiresPrecision: true, nativeName: "decimal")
    {
    }
}
