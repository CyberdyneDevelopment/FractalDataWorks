using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>text</c> — maps to abstract type <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "text")]
public sealed class TextType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TextType"/> class.</summary>
    public TextType()
        : base(id: 4, name: "text")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.String;
}
