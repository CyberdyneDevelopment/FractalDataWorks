using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// Base class for a <see cref="CalculationSourceTypes"/> option. Each concrete source owns its own
/// resolution strategy — see <c>extract-typecollection-strategy</c> (behavioral TypeOption per option).
/// </summary>
public abstract class CalculationSourceTypeBase : TypeOptionBase<int, CalculationSourceTypeBase>, ICalculationSourceType
{
    /// <summary>Initializes a new instance of the <see cref="CalculationSourceTypeBase"/> class.</summary>
    /// <param name="id">The unique identifier for this source.</param>
    /// <param name="name">The source name (also the provenance value stamped on written records).</param>
    protected CalculationSourceTypeBase(int id, string name) : base(id, name)
    {
    }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<IReadOnlyList<CalculationCatalogItem>>> List(
        CalculationSourceContext context,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IGenericResult<CalculationCatalogItem>> Resolve(
        string name,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IGenericResult<CalculationCatalogItem>> Resolve(
        Guid id,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default);
}
