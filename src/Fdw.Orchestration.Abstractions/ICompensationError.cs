using System;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Represents a compensation error.
/// </summary>
public interface ICompensationError
{
    /// <summary>
    /// Gets the step ID that failed compensation.
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    string ErrorMessage { get; }

    /// <summary>
    /// Gets the exception.
    /// </summary>
    Exception? Exception { get; }
}
