using System;
using System.Collections.Generic;
using Fdw.Commands.Data;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// What a data set's store runs for a query, and which steps are left to run over the rows it returns.
/// </summary>
internal sealed class PushdownPlan
{
    /// <summary>Initializes a new instance of the <see cref="PushdownPlan"/> class.</summary>
    /// <param name="command">The query the store runs.</param>
    /// <param name="steps">The steps left to run in memory.</param>
    public PushdownPlan(QueryCommand<Dictionary<string, object?>> command, ProjectionSteps steps)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Steps = steps;
    }

    /// <summary>Gets the query the store runs.</summary>
    public QueryCommand<Dictionary<string, object?>> Command { get; }

    /// <summary>Gets the steps left to run in memory.</summary>
    public ProjectionSteps Steps { get; }
}
