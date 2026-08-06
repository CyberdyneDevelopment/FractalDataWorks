using System;
using System.Collections.Generic;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Result of compensation execution.
/// </summary>
public interface ICompensationResult
{
    /// <summary>
    /// Gets whether compensation was successful.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the number of steps compensated.
    /// </summary>
    int StepsCompensated { get; }

    /// <summary>
    /// Gets compensation errors.
    /// </summary>
    IReadOnlyList<ICompensationError> Errors { get; }

    /// <summary>
    /// Gets compensation duration.
    /// </summary>
    TimeSpan Duration { get; }
}
