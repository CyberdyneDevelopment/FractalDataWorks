using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>nvarchar</c> — normalizes to <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "nvarchar")]
public sealed class NVarCharType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="NVarCharType"/> class.</summary>
    public NVarCharType()
        : base(
            id: 6,
            name: "nvarchar",
            description: "Variable-length Unicode character data.",
            abstractType: DataTypes.String,
            isUnicode: true, isVariableLength: true, maxLength: 4000, requiresLength: true, defaultLength: 1, supportsStreaming: true)
    {
    }
}
