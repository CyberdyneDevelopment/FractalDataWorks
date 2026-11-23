using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Data;

/// <summary>The base of every SQL aggregate function.</summary>
[ExcludeFromCodeCoverage]
public abstract class SqlAggregateBase : TypeOptionBase<SqlAggregateBase>, ISqlAggregate
{
    /// <summary>Initializes a new instance of the <see cref="SqlAggregateBase"/> class.</summary>
    /// <param name="name">The function's name.</param>
    /// <param name="sql">The keyword the query translator emits.</param>
    /// <param name="queryName">The name a data query request states it by.</param>
    /// <param name="aliases">The words a data set's aggregate definition may state it by, in upper case.</param>
    protected SqlAggregateBase(string name, string sql, string queryName, string[] aliases)
        : base(name)
    {
        Sql = sql;
        QueryName = queryName;
        Aliases = aliases;
    }

    /// <inheritdoc />
    public string Sql { get; }

    /// <inheritdoc />
    public string QueryName { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; }

    /// <inheritdoc />
    public abstract string ResultType(string fieldTypeName);
}
