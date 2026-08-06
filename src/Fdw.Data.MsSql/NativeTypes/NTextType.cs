using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>ntext</c> — normalizes to <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "ntext")]
public sealed class NTextType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="NTextType"/> class.</summary>
    public NTextType()
        : base(
            id: 10,
            name: "ntext",
            description: "Variable-length Unicode data. Superseded by nvarchar(max).",
            abstractType: DataTypes.String,
            isUnicode: true, supportsStreaming: true, isDeprecated: true)
    {
    }
}
