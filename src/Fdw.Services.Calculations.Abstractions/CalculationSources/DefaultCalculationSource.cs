using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Abstractions;
using Fdw.Calculations.Abstractions.CalculationTypeOptions;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.ResultCodes;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// The codified calculation source — surfaces the ship-with-code scalar aggregation operators
/// (Sum, Average, Count, Min, Max, Percentile) declared in <see cref="CalculationTypes"/>
/// (<c>Fdw.Calculations.Abstractions</c>). No <c>calc.CalculationEntity</c> rows are ever written
/// for this source; the operators live entirely in code.
/// </summary>
[TypeOption(typeof(CalculationSourceTypes), "Default")]
public sealed class DefaultCalculationSource : CalculationSourceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultCalculationSource"/> class.</summary>
    public DefaultCalculationSource() : base(1, "Default")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<IReadOnlyList<CalculationCatalogItem>>> List(
        CalculationSourceContext context,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CalculationCatalogItem> items = CalculationTypes.All()
            .Select(ToCatalogItem)
            .ToList();

        return Task.FromResult<IGenericResult<IReadOnlyList<CalculationCatalogItem>>>(
            GenericResult<IReadOnlyList<CalculationCatalogItem>>.Success(items));
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<CalculationCatalogItem>> Resolve(
        string name,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default)
    {
        var operatorType = CalculationTypes.ByName(name);
        if (operatorType == CalculationTypes.NotFound)
        {
            return Task.FromResult<IGenericResult<CalculationCatalogItem>>(
                GenericResult<CalculationCatalogItem>.Failure(
                    CalculationEntityResultCodes.ByName("CalculationNotFound"),
                    ResultDetails.Create("Name", name)));
        }

        return Task.FromResult<IGenericResult<CalculationCatalogItem>>(
            GenericResult<CalculationCatalogItem>.Success(ToCatalogItem(operatorType)));
    }

    /// <inheritdoc/>
    // Why: codified operators have no calc.CalculationEntity row — there is no Guid to resolve against.
    public override Task<IGenericResult<CalculationCatalogItem>> Resolve(
        Guid id,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult<CalculationCatalogItem>>(
            GenericResult<CalculationCatalogItem>.Failure(
                CalculationEntityResultCodes.ByName("CalculationNotFound"),
                ResultDetails.Create("Name", id.ToString())));
    }

    // Why: a codified scalar operator has no configured output field — it always produces exactly
    // one value, labeled by the operator's own name (Sum/Average/...); not a fabricated placeholder.
    private CalculationCatalogItem ToCatalogItem(ICalculationType operatorType) => new()
    {
        Name = operatorType.Name,
        DisplayName = operatorType.Name,
        CalculationSource = Name,
        OperatorId = operatorType.Id,
        CalculationEntityId = null,
        OutputField = operatorType.Name,
        IsEnabled = true
    };
}
