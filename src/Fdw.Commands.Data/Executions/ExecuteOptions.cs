using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Commands.Data.Executions;

/// <summary>
/// The configuration of an execute: the value source of each parameter of the executable container the command
/// runs against.
/// </summary>
/// <remarks>
/// What is executed is the container the command is dispatched with (its data store's own container metadata),
/// never text the caller supplies. What "executing" it means is decided only by the connection type's translator.
/// </remarks>
public sealed class ExecuteOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteOptions"/> class.
    /// </summary>
    /// <param name="parameters">The parameters being supplied; may be empty.</param>
    public ExecuteOptions(IReadOnlyList<ExecuteParameter> parameters)
    {
        Parameters = parameters;
    }

    /// <summary>Gets the parameters being supplied.</summary>
    public IReadOnlyList<ExecuteParameter> Parameters { get; }

    /// <summary>
    /// Describes the first thing wrong with these options, or returns <see langword="null"/> when they are
    /// consistent. Names are checked by the translator against the executable container's fields.
    /// </summary>
    /// <returns>A plain-language reason, or <see langword="null"/>.</returns>
    public string? FirstProblem()
    {
        if (Parameters.Any(p => string.IsNullOrWhiteSpace(p.Name)))
            return "every execute parameter names its container field";
        var twice = Parameters.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        return twice is null
            ? null
            : $"parameter '{twice.Key}' is given twice";
    }
}
