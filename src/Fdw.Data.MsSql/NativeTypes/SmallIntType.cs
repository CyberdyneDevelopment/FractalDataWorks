using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>smallint</c> — normalizes to <see cref="DataTypes.Int16"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "smallint")]
public sealed class SmallIntType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallIntType"/> class.</summary>
    public SmallIntType()
        : base(
            id: 3,
            name: "smallint",
            description: "16-bit signed integer.",
            abstractType: DataTypes.Int16,
            isNumeric: true, maxPrecision: 5)
    {
    }
}
