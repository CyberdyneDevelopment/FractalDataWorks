using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// In-memory registry of loaded calculation entities.
/// Supports lookup by name or id and supports dynamic registration at startup.
/// </summary>
public interface ICalculationEntityProvider
{
    /// <summary>Gets a calculation entity by name.</summary>
    Task<IGenericResult<ICalculationEntity>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a calculation entity by its unique identifier.</summary>
    Task<IGenericResult<ICalculationEntity>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all registered calculation entities.</summary>
    Task<IGenericResult<IReadOnlyList<ICalculationEntity>>> Get(CancellationToken cancellationToken = default);
}
