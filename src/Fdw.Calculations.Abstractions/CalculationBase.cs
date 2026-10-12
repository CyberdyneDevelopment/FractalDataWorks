using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Results;
using Fdw.Results;

namespace Fdw.Calculations;

/// <summary>
/// Base class for calculation implementations.
/// Provides common functionality and enforces consistent patterns.
/// </summary>
/// <typeparam name="TInput">The input data type.</typeparam>
/// <typeparam name="TOutput">The output data type.</typeparam>
/// <ExcludeFromCodeCoverageReason>Base class with simple property assignments</ExcludeFromCodeCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class CalculationBase<TInput, TOutput> : ICalculation<TInput, TOutput>
{
    private static readonly IReadOnlyList<string> EmptyList = Array.Empty<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationBase{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="calculationId">The unique calculation identifier.</param>
    /// <param name="name">The calculation name.</param>
    /// <param name="calculationType">The calculation type.</param>
    /// <param name="description">The calculation description.</param>
    protected CalculationBase(
        string calculationId,
        string name,
        string calculationType,
        string? description = null)
    {
        CalculationId = calculationId ?? throw new ArgumentNullException(nameof(calculationId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CalculationType = calculationType ?? throw new ArgumentNullException(nameof(calculationType));
        Description = description;
    }

    /// <inheritdoc/>
    public string CalculationId { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string? Description { get; }

    /// <inheritdoc/>
    public string CalculationType { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Override this property to declare datasets required by this calculation.
    /// Default implementation returns an empty list.
    /// </remarks>
    public virtual IReadOnlyList<string> RequiredDataSets => EmptyList;

    /// <inheritdoc/>
    /// <remarks>
    /// Override this property to declare other calculations this calculation depends on.
    /// Dependent calculations must execute first and store results in context.SharedState.
    /// Default implementation returns an empty list.
    /// </remarks>
    public virtual IReadOnlyList<string> DependsOn => EmptyList;

    /// <inheritdoc/>
    public abstract Task<IGenericResult<TOutput>> Execute(
        TInput input,
        ICalculationContext context,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IGenericResult> Validate(
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual IGenericResult ValidateDependencies(ICalculationContext context)
    {
        var missingDataSets = RequiredDataSets
            .Where(ds => !context.SharedState.ContainsKey($"DataSet:{ds}"))
            .ToList();

        var missingCalculations = DependsOn
            .Where(calc => !context.SharedState.ContainsKey($"Calculation:{calc}"))
            .ToList();

        if (missingDataSets.Count == 0 && missingCalculations.Count == 0)
        {
            return GenericResult.Success();
        }

        var errors = new List<string>();

        if (missingDataSets.Count > 0)
        {
            errors.Add($"Missing required datasets: {string.Join(", ", missingDataSets)}");
        }

        if (missingCalculations.Count > 0)
        {
            errors.Add($"Missing dependent calculations: {string.Join(", ", missingCalculations)}");
        }

        return GenericResult.Failure(
            CalculationResultCodes.ByName("DependencyValidationFailed"),
            ResultDetails.Create().With("Errors", string.Join("; ", errors)));
    }
}
