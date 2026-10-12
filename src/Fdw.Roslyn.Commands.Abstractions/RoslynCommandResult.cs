using System;
using System.Collections.Generic;
using Fdw.Commands.Development.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Default implementation of <see cref="IRoslynCommandResult"/>.
/// </summary>
public class RoslynCommandResult : DevelopmentCommandResult, IRoslynCommandResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="RoslynCommandResult"/>.
    /// </summary>
    /// <param name="summary">The result summary.</param>
    /// <param name="data">The structured data.</param>
    public RoslynCommandResult(string summary, IReadOnlyDictionary<string, object>? data = null)
        : base(summary, data)
    {
    }

    /// <inheritdoc/>
    public bool IsMutation => false;

    /// <inheritdoc/>
    public Solution? NewSolution => null;

    /// <summary>
    /// Creates a success result with a summary message.
    /// </summary>
    public new static RoslynCommandResult Success(string summary) =>
        new(summary);

    /// <summary>
    /// Creates a success result with data.
    /// </summary>
    public new static RoslynCommandResult Success(string summary, IReadOnlyDictionary<string, object> data) =>
        new(summary, data);
}
