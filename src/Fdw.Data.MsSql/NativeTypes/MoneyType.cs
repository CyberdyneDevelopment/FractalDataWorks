using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>money</c> — normalizes to <see cref="DataTypes.Decimal"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "money")]
public sealed class MoneyType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="MoneyType"/> class.</summary>
    public MoneyType()
        : base(
            id: 18,
            name: "money",
            description: "Currency value with four decimal places.",
            abstractType: DataTypes.Decimal,
            isNumeric: true, maxPrecision: 19, maxScale: 4, defaultPrecision: 19, defaultScale: 4)
    {
    }
}
