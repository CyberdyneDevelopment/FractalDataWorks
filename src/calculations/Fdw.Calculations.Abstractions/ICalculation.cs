using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Calculations;

/// <summary>
/// Represents a calculation that transforms input data into output data.
/// Calculations follow Railway-Oriented Programming pattern with IGenericResult.
/// </summary>
/// <typeparam name="TInput">The input data type.</typeparam>
/// <typeparam name="TOutput">The output data type.</typeparam>
public interface ICalculation<TInput, TOutput>
{
    /// <summary>
    /// Gets the unique identifier for this calculation instance.
    /// </summary>
    string CalculationId { get; }

    /// <summary>
    /// Gets the name of this calculation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this calculation.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the calculation type (Aggregation, PeriodComparison, Custom, etc.).
    /// </summary>
    string CalculationType { get; }

    /// <summary>
    /// Gets the names of datasets required by this calculation.
    /// Used for validation and dependency graph construction.
    /// </summary>
    IReadOnlyList<string> RequiredDataSets { get; }

    /// <summary>
    /// Gets the names of other calculations this calculation depends on.
    /// Dependent calculations must execute first and store results in context.State.
    /// </summary>
    IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Executes the calculation with the provided input and context.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <param name="context">The calculation execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the calculated output or failure information.</returns>
    Task<IGenericResult<TOutput>> Execute(
        TInput input,
        ICalculationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the calculation configuration and requirements.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure with validation messages.</returns>
    Task<IGenericResult> Validate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all required datasets and dependent calculations are available in the context.
    /// </summary>
    /// <param name="context">The calculation context to validate against.</param>
    /// <returns>A result indicating success or failure with missing dependencies.</returns>
    IGenericResult ValidateDependencies(ICalculationContext context);
}
