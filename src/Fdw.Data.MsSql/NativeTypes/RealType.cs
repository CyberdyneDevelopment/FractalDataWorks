using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>real</c> — normalizes to <see cref="DataTypes.Float"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "real")]
public sealed class RealType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="RealType"/> class.</summary>
    public RealType()
        : base(
            id: 17,
            name: "real",
            description: "Single-precision floating-point number.",
            abstractType: DataTypes.Float,
            isNumeric: true, maxPrecision: 24, defaultPrecision: 24)
    {
    }
}
