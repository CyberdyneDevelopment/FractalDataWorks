using System;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Represents a query result that does not modify the solution.
/// </summary>
/// <typeparam name="T">The type of data returned by the query.</typeparam>
public sealed class QueryResult<T> : IRoslynCommandResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryResult{T}"/> class.
    /// </summary>
    /// <param name="summary">A summary of the result.</param>
    /// <param name="data">The query result data.</param>
    public QueryResult(string summary, T data)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        Data = data;
    }

    /// <inheritdoc/>
    public string Summary { get; }

    /// <inheritdoc/>
    public bool IsMutation => false;

    /// <inheritdoc/>
    public Solution? NewSolution => null;

    /// <summary>
    /// Gets the query result data.
    /// </summary>
    public T Data { get; }
}
