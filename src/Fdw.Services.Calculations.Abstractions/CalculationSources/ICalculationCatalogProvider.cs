using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// Surfaces the unified calculation catalog — the union of every registered
/// <see cref="CalculationSourceTypes"/> option's entries — through one provenance-qualified provider.
/// Replaces <c>ICalculationProvider</c>.
/// </summary>
/// <remarks>
/// Why source-qualified: a bare <c>Get(name)</c> would reintroduce cross-source name precedence
/// (which source wins on a name collision). Callers that need a specific entry name the source
/// explicitly via <see cref="Get(string,string,CancellationToken)"/>.
/// </remarks>
public interface ICalculationCatalogProvider
{
    /// <summary>Returns the full catalog union across every registered <see cref="CalculationSourceTypes"/> option.</summary>
    Task<IGenericResult<IReadOnlyList<CalculationCatalogItem>>> Get(
        CancellationToken cancellationToken = default);

    /// <summary>Resolves a catalog item by its <c>calc.CalculationEntity</c> id, trying each source in turn.</summary>
    Task<IGenericResult<CalculationCatalogItem>> Get(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves a catalog item by explicit source name and item name.</summary>
    Task<IGenericResult<CalculationCatalogItem>> Get(
        string source,
        string name,
        CancellationToken cancellationToken = default);
}
