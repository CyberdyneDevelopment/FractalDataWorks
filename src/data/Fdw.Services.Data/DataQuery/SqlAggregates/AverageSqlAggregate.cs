using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>The Average aggregate.</summary>
[TypeOption(typeof(SqlAggregates), "Average")]
[ExcludeFromCodeCoverage]
public sealed class AverageSqlAggregate : SqlAggregateBase
{
    /// <summary>Initializes a new instance of the <see cref="AverageSqlAggregate"/> class.</summary>
    public AverageSqlAggregate()
        : base("Average", "AVG", "Avg", ["AVG", "AVERAGE", "MEAN"])
    {
    }

    /// <inheritdoc />
    public override string ResultType(string fieldTypeName) => DataTypes.Decimal.Name;
}
