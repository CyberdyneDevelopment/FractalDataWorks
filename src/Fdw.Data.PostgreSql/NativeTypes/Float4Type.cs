using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>float4</c> — maps to abstract type <see cref="DataTypes.Float"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "float4")]
public sealed class Float4Type : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Float4Type"/> class.</summary>
    public Float4Type()
        : base(id: 12, name: "float4")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Float;
}
