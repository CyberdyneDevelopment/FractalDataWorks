using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>Char</c> — normalizes to <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "Char")]
public sealed class CharType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="CharType"/> class.</summary>
    public CharType()
        : base(
            id: 7,
            name: "Char",
            description: "Fixed-length non-Unicode character data.",
            abstractType: DataTypes.String,
            maxLength: 8000, requiresLength: true, defaultLength: 1)
    {
    }
}
