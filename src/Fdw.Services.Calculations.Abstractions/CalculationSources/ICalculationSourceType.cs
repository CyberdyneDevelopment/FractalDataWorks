using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// A calculation catalog origin (e.g. "Default", "Configuration") that owns its own resolution —
/// each option knows how to list and resolve the calculations it is the source of truth for.
/// </summary>
public interface ICalculationSourceType : ITypeOption<int, CalculationSourceTypeBase>
{
    /// <summary>Lists every catalog item this source currently owns.</summary>
    Task<IGenericResult<IReadOnlyList<CalculationCatalogItem>>> List(
        CalculationSourceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves a single catalog item owned by this source by name.</summary>
    Task<IGenericResult<CalculationCatalogItem>> Resolve(
        string name,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves a single catalog item owned by this source by its <c>calc.CalculationEntity</c> id.</summary>
    Task<IGenericResult<CalculationCatalogItem>> Resolve(
        Guid id,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default);
}
