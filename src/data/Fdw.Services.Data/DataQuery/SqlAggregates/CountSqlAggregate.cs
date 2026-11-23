using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>The Count aggregate.</summary>
[TypeOption(typeof(SqlAggregates), "Count")]
[ExcludeFromCodeCoverage]
public sealed class CountSqlAggregate : SqlAggregateBase
{
    /// <summary>Initializes a new instance of the <see cref="CountSqlAggregate"/> class.</summary>
    public CountSqlAggregate()
        : base("Count", "COUNT", "Count", ["COUNT"])
    {
    }

    /// <inheritdoc />
    public override string ResultType(string fieldTypeName) => DataTypes.Int64.Name;
}
