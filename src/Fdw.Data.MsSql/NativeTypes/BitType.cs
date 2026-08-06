using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>bit</c> — normalizes to <see cref="DataTypes.Bool"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "bit")]
public sealed class BitType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="BitType"/> class.</summary>
    public BitType()
        : base(
            id: 19,
            name: "bit",
            description: "Single bit — 0, 1 or null.",
            abstractType: DataTypes.Bool)
    {
    }
}
