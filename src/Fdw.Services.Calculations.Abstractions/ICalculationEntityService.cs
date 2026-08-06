using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Service for managing and executing calculation entities.
/// </summary>
public interface ICalculationEntityService
{
    /// <summary>Gets a calculation entity by name.</summary>
    Task<IGenericResult<ICalculationEntity>> GetCalculation(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a calculation entity by its unique identifier.</summary>
    Task<IGenericResult<ICalculationEntity>> GetCalculationById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all registered calculation entities.</summary>
    Task<IGenericResult<IReadOnlyList<ICalculationEntity>>> ListCalculations(CancellationToken cancellationToken = default);

    /// <summary>Validates that a calculation entity's configuration is correct.</summary>
    Task<IGenericResult> ValidateCalculation(ICalculationEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Creates a new calculation entity and returns it. If <paramref name="typedConfiguration"/> is provided, persists the type-specific body row.</summary>
    Task<IGenericResult<ICalculationEntity>> CreateCalculation(
        string name,
        string? description,
        string calculationEntityType,
        IReadOnlyList<CalculationInput> inputs,
        CalculationOutputSpec output,
        IGenericConfiguration? typedConfiguration = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing calculation entity (version-on-write). If <paramref name="typedConfiguration"/> is provided, persists the type-specific body row.</summary>
    Task<IGenericResult<ICalculationEntity>> UpdateCalculation(
        Guid id,
        string name,
        string? description,
        string calculationEntityType,
        IReadOnlyList<CalculationInput> inputs,
        CalculationOutputSpec output,
        bool isEnabled,
        IGenericConfiguration? typedConfiguration = null,
        CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a calculation entity by ID.</summary>
    Task<IGenericResult> DeleteCalculation(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Executes a named calculation and returns the serialized result.</summary>
    Task<IGenericResult<string>> ExecuteCalculation(string calculationName, ICalculationContext context, CancellationToken cancellationToken = default);
}
