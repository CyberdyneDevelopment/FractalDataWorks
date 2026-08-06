using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>bool</c> — maps to abstract type <see cref="DataTypes.Bool"/>.
/// </summary>
/// <remarks>
/// Why: TypeOption name uses PascalCase "Bool" to avoid a C# reserved-keyword conflict in
/// source-generated property names. Use <c>PostgreSqlNativeTypes.ByName("Bool")</c> for lookup.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "Bool")]
public sealed class BoolType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BoolType"/> class.</summary>
    public BoolType()
        : base(id: 14, name: "Bool")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Bool;
}
