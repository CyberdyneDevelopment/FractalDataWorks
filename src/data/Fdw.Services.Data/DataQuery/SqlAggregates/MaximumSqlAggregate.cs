using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>The Maximum aggregate.</summary>
[TypeOption(typeof(SqlAggregates), "Maximum")]
[ExcludeFromCodeCoverage]
public sealed class MaximumSqlAggregate : SqlAggregateBase
{
    /// <summary>Initializes a new instance of the <see cref="MaximumSqlAggregate"/> class.</summary>
    public MaximumSqlAggregate()
        : base("Maximum", "MAX", "Max", ["MAX", "MAXIMUM"])
    {
    }

    /// <inheritdoc />
    public override string ResultType(string fieldTypeName) => fieldTypeName;
}
