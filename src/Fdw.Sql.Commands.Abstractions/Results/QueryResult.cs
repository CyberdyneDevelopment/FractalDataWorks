using System;

namespace Fdw.Sql.Commands.Abstractions.Results;

/// <summary>Read-only result returned from non-mutating SQL commands.</summary>
public sealed class QueryResult<T> : ISqlCommandResult
{
    public QueryResult(string summary, T data)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        Data = data;
    }

    /// <inheritdoc/>
    public string Summary { get; }

    /// <inheritdoc/>
    public bool IsMutation => false;

    /// <summary>Result payload.</summary>
    public T Data { get; }
}
