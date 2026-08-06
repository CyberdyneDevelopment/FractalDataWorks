using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>char</c> — maps to abstract type <see cref="DataTypes.String"/>.
/// </summary>
/// <remarks>
/// Why: TypeOption name uses PascalCase "Char" to avoid a C# reserved-keyword conflict in
/// source-generated property names. Use <c>PostgreSqlNativeTypes.ByName("Char")</c> for lookup.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "Char")]
public sealed class CharType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CharType"/> class.</summary>
    public CharType()
        : base(id: 6, name: "Char")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.String;
}
