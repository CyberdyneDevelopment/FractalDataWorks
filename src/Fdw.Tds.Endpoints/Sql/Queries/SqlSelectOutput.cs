using System;
using System.Collections.Generic;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// What a SELECT produced: a result set to send, or variables assigned and nothing to send.
/// </summary>
public sealed class SqlSelectOutput
{
    /// <summary>Initializes a new instance of the <see cref="SqlSelectOutput"/> class.</summary>
    /// <param name="columns">The result's columns.</param>
    /// <param name="rows">The result's rows, one value per column.</param>
    /// <param name="assignedOnly">Whether the SELECT assigned variables and has no result set.</param>
    public SqlSelectOutput(IReadOnlyList<SqlOutputColumn> columns, IReadOnlyList<object?[]> rows, bool assignedOnly)
    {
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        AssignedOnly = assignedOnly;
    }

    /// <summary>Gets the result's columns.</summary>
    public IReadOnlyList<SqlOutputColumn> Columns { get; }

    /// <summary>Gets the result's rows.</summary>
    public IReadOnlyList<object?[]> Rows { get; }

    /// <summary>Gets a value indicating whether the SELECT assigned variables and has no result set.</summary>
    public bool AssignedOnly { get; }
}
