using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>tinyint</c> — normalizes to <see cref="DataTypes.Byte"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "tinyint")]
public sealed class TinyIntType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="TinyIntType"/> class.</summary>
    public TinyIntType()
        : base(
            id: 4,
            name: "tinyint",
            description: "8-bit unsigned integer.",
            abstractType: DataTypes.Byte,
            isNumeric: true, maxPrecision: 3)
    {
    }
}
