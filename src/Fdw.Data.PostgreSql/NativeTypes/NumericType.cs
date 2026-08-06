using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>numeric</c> — maps to abstract type <see cref="DataTypes.Decimal"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "numeric")]
public sealed class NumericType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NumericType"/> class.</summary>
    public NumericType()
        : base(id: 11, name: "numeric")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Decimal;
}
