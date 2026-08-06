using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>text</c> — normalizes to <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "text")]
public sealed class TextType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="TextType"/> class.</summary>
    public TextType()
        : base(
            id: 9,
            name: "text",
            description: "Variable-length non-Unicode data. Superseded by varchar(max).",
            abstractType: DataTypes.String,
            supportsStreaming: true, isDeprecated: true)
    {
    }
}
