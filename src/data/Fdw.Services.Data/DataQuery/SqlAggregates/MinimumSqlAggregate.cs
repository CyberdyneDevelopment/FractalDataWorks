using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>The Minimum aggregate.</summary>
[TypeOption(typeof(SqlAggregates), "Minimum")]
[ExcludeFromCodeCoverage]
public sealed class MinimumSqlAggregate : SqlAggregateBase
{
    /// <summary>Initializes a new instance of the <see cref="MinimumSqlAggregate"/> class.</summary>
    public MinimumSqlAggregate()
        : base("Minimum", "MIN", "Min", ["MIN", "MINIMUM"])
    {
    }

    /// <inheritdoc />
    public override string ResultType(string fieldTypeName) => fieldTypeName;
}
