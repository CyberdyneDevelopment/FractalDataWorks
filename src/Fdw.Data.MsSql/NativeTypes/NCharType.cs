using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>nchar</c> — normalizes to <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "nchar")]
public sealed class NCharType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="NCharType"/> class.</summary>
    public NCharType()
        : base(
            id: 8,
            name: "nchar",
            description: "Fixed-length Unicode character data.",
            abstractType: DataTypes.String,
            isUnicode: true, maxLength: 4000, requiresLength: true, defaultLength: 1)
    {
    }
}
