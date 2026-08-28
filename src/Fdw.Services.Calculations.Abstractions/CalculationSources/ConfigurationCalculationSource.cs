using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.ResultCodes;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// The configured calculation source — surfaces <c>calc.CalculationEntity</c> rows written through
/// <see cref="ICalculationEntityService"/>, filtered to the rows this source itself wrote (its
/// <c>CalculationSource</c> column equals this option's own <c>Name</c>).
/// </summary>
[TypeOption(typeof(CalculationSourceTypes), "Configuration")]
public sealed class ConfigurationCalculationSource : CalculationSourceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationCalculationSource"/> class.</summary>
    public ConfigurationCalculationSource() : base(2, "Configuration")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IReadOnlyList<CalculationCatalogItem>>> List(
        CalculationSourceContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await context.EntityService.ListCalculations(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyList<CalculationCatalogItem>>();

        IReadOnlyList<CalculationCatalogItem> items = (result.Value ?? [])
            .Where(e => string.Equals(e.CalculationSource, Name, StringComparison.Ordinal))
            .Select(ToCatalogItem)
            .ToList();

        return GenericResult<IReadOnlyList<CalculationCatalogItem>>.Success(items);
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<CalculationCatalogItem>> Resolve(
        string name,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await context.EntityService.GetCalculation(name, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.ToNewResult<CalculationCatalogItem>();

        var entity = result.Value!;
        if (!string.Equals(entity.CalculationSource, Name, StringComparison.Ordinal))
        {
            return GenericResult<CalculationCatalogItem>.Failure(
                CalculationEntityResultCodes.ByName("CalculationNotFound"),
                ResultDetails.Create("Name", name));
        }

        return GenericResult<CalculationCatalogItem>.Success(ToCatalogItem(entity));
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<CalculationCatalogItem>> Resolve(
        Guid id,
        CalculationSourceContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await context.EntityService.GetCalculationById(id, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.ToNewResult<CalculationCatalogItem>();

        var entity = result.Value!;
        if (!string.Equals(entity.CalculationSource, Name, StringComparison.Ordinal))
        {
            return GenericResult<CalculationCatalogItem>.Failure(
                CalculationEntityResultCodes.ByName("CalculationNotFound"),
                ResultDetails.Create("Name", id.ToString()));
        }

        return GenericResult<CalculationCatalogItem>.Success(ToCatalogItem(entity));
    }

    private CalculationCatalogItem ToCatalogItem(ICalculationEntity entity) => new()
    {
        Name = entity.Name,
        DisplayName = entity.Name,
        Description = entity.Description,
        CalculationSource = entity.CalculationSource,
        CalculationEntityId = entity.Id,
        OperatorId = null,
        RequiredInputFields = entity.Inputs.Select(i => i.InputAlias).ToList(),
        OutputField = entity.Output.ResultFieldName,
        IsEnabled = entity.IsEnabled
    };
}
