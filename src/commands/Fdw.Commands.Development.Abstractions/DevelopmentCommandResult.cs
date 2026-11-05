using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Default implementation of <see cref="IDevelopmentCommandResult"/>.
/// </summary>
public class DevelopmentCommandResult : IDevelopmentCommandResult
{
    /// <inheritdoc/>
    public string Summary { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DevelopmentCommandResult"/>.
    /// </summary>
    /// <param name="summary">The result summary.</param>
    /// <param name="data">The structured data.</param>
    public DevelopmentCommandResult(string summary, IReadOnlyDictionary<string, object>? data = null)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        Data = data ?? ImmutableDictionary<string, object>.Empty;
    }

    /// <summary>
    /// Creates a success result with a summary message.
    /// </summary>
    public static DevelopmentCommandResult Success(string summary) =>
        new(summary);

    /// <summary>
    /// Creates a success result with data.
    /// </summary>
    public static DevelopmentCommandResult Success(string summary, IReadOnlyDictionary<string, object> data) =>
        new(summary, data);
}
