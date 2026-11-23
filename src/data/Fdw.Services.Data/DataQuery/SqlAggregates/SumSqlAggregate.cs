using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>The Sum aggregate.</summary>
[TypeOption(typeof(SqlAggregates), "Sum")]
[ExcludeFromCodeCoverage]
public sealed class SumSqlAggregate : SqlAggregateBase
{
    /// <summary>Initializes a new instance of the <see cref="SumSqlAggregate"/> class.</summary>
    public SumSqlAggregate()
        : base("Sum", "SUM", "Sum", ["SUM"])
    {
    }

    /// <inheritdoc />
    public override string ResultType(string fieldTypeName) => DataTypes.Decimal.Name;
}
