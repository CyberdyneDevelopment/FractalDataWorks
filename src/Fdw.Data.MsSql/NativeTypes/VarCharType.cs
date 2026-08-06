using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>varchar</c> — normalizes to <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "varchar")]
public sealed class VarCharType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="VarCharType"/> class.</summary>
    public VarCharType()
        : base(
            id: 5,
            name: "varchar",
            description: "Variable-length non-Unicode character data.",
            abstractType: DataTypes.String,
            isVariableLength: true, maxLength: 8000, requiresLength: true, defaultLength: 1, supportsStreaming: true)
    {
    }
}
