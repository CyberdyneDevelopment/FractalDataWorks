using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>float8</c> — maps to abstract type <see cref="DataTypes.Double"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "float8")]
public sealed class Float8Type : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Float8Type"/> class.</summary>
    public Float8Type()
        : base(id: 13, name: "float8")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Double;
}
