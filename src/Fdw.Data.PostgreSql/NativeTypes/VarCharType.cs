using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>varchar</c> — maps to abstract type <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "varchar")]
public sealed class VarCharType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="VarCharType"/> class.</summary>
    public VarCharType()
        : base(id: 5, name: "varchar")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.String;
}
