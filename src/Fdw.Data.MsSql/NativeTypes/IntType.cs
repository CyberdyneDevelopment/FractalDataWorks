using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>int</c> — normalizes to <see cref="DataTypes.Int32"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "Int")]
public sealed class IntType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="IntType"/> class.</summary>
    public IntType()
        : base(
            id: 2,
            name: "Int",
            description: "32-bit signed integer.",
            abstractType: DataTypes.Int32,
            isNumeric: true, maxPrecision: 10, nativeName: "int")
    {
    }
}
