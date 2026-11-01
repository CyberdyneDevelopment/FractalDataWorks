using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>smallmoney</c> — normalizes to <see cref="DataTypes.Decimal"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "smallmoney")]
public sealed class SmallMoneyType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallMoneyType"/> class.</summary>
    public SmallMoneyType()
        : base(
            name: "smallmoney",
            description: "Currency value with four decimal places, in a smaller range than money.",
            abstractType: DataTypes.Decimal,
            isNumeric: true, maxPrecision: 10, maxScale: 4, defaultPrecision: 10, defaultScale: 4)
    {
    }
}
