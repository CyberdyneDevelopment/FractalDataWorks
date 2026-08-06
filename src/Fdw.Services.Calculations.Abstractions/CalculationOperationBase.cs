using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Abstract base class for all calculation operations (CRTP pattern).
/// Provides common metadata (category, description, parameters) and requires
/// subclasses to implement <see cref="Calculate"/>.
/// </summary>
public abstract class CalculationOperationBase : TypeOptionBase<int, CalculationOperationBase>, ICalculationOperation
{
    /// <summary>
    /// Gets the category this operation belongs to (e.g., "Arithmetic", "Aggregate", "Window").
    /// </summary>
    public new string Category { get; }

    /// <summary>
    /// Gets a human-readable description of what this operation does.
    /// </summary>
    public new string Description { get; }

    /// <summary>
    /// Gets the parameter definitions this operation accepts.
    /// </summary>
    public IReadOnlyList<OperationParameterDefinition> Parameters { get; protected init; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationOperationBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this operation.</param>
    /// <param name="name">The name of this operation (e.g., "Add", "Sum").</param>
    /// <param name="category">The category grouping (e.g., "Arithmetic", "Aggregate").</param>
    /// <param name="description">A human-readable description of the operation.</param>
    protected CalculationOperationBase(int id, string name, string category, string description)
        : base(id, name)
    {
        Category = category;
        Description = description;
    }

    /// <inheritdoc />
    public abstract Task<IGenericResult<object>> Calculate(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
