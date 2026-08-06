using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Represents a composable calculation operation that can be used as a step
/// in a calculation pipeline. Each operation declares its parameters and
/// provides an execution method.
/// </summary>
public interface ICalculationOperation : ITypeOption<int, CalculationOperationBase>
{
    /// <summary>
    /// Gets the category this operation belongs to (e.g., "Arithmetic", "Aggregate", "Window").
    /// Used for grouping operations in the UI.
    /// </summary>
    new string Category { get; }

    /// <summary>
    /// Gets a human-readable description of what this operation does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the parameter definitions this operation accepts.
    /// Each entry describes a named parameter with its kind, display name, and optionality.
    /// </summary>
    IReadOnlyList<OperationParameterDefinition> Parameters { get; }

    /// <summary>
    /// Executes the operation with the supplied parameter values.
    /// </summary>
    /// <param name="parameters">
    /// A dictionary mapping parameter names to their resolved values.
    /// Keys correspond to <see cref="OperationParameterDefinition.Name"/>.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A result containing the computed value on success, or a failure message on error.
    /// </returns>
    Task<IGenericResult<object>> Calculate(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
