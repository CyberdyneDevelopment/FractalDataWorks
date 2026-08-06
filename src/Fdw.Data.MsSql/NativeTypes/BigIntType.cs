using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>bigint</c> — normalizes to <see cref="DataTypes.Int64"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "bigint")]
public sealed class BigIntType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="BigIntType"/> class.</summary>
    public BigIntType()
        : base(
            id: 1,
            name: "bigint",
            description: "64-bit signed integer.",
            abstractType: DataTypes.Int64,
            isNumeric: true, maxPrecision: 19)
    {
    }
}
