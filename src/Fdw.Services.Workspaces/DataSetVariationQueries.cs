using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>Reads data set variations: every one, or the ones a workspace owns.</summary>
/// <remarks>One domain query on the variation table: nothing is composed, so it costs one read however many there are.</remarks>
public static class DataSetVariationQueries
{
    private const int AllRows = int.MaxValue - 1;

    /// <summary>The variations the workspace owns, as domain rows.</summary>
    /// <param name="variations">The variation provider.</param>
    /// <param name="workspaceId">The logical id of the owning workspace.</param>
    /// <param name="ct">The cancellation token.</param>
    public static Task<IGenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>> OwnedBy(
        IDataSetVariationConfigurationProvider variations, Guid workspaceId, CancellationToken ct)
        => Read(
            variations,
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = nameof(DataSetVariationDomainConfiguration.OwnerWorkspaceId),
                        Operator = FilterOperators.ByName("Equal"),
                        Value = workspaceId,
                    },
                },
                null, 0, AllRows, false),
            ct);

    /// <summary>Every variation, as domain rows, whoever owns it.</summary>
    /// <param name="variations">The variation provider.</param>
    /// <param name="ct">The cancellation token.</param>
    public static Task<IGenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>> All(
        IDataSetVariationConfigurationProvider variations, CancellationToken ct)
        => Read(variations, new DomainQuery(null, null, 0, AllRows, false), ct);

    /// <summary>The variation the workspace owns under this name, or nothing when it owns none.</summary>
    /// <param name="variations">The variation provider.</param>
    /// <param name="workspaceId">The logical id of the owning workspace.</param>
    /// <param name="variationName">The name the workspace knows the variation by.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <remarks>
    /// One query on the variation table, by the owner and the name, so nothing parses the variation's key. A variation
    /// shadows an attached data set of the same name; creating one refuses a name any data set already has, so the two
    /// never meet.
    /// </remarks>
    public static async Task<IGenericResult<DataSetVariationDomainConfiguration?>> Named(
        IDataSetVariationConfigurationProvider variations, Guid workspaceId, string variationName, CancellationToken ct)
    {
        var page = await variations.Get(
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterGroup
                    {
                        Operator = LogicalOperator.And,
                        Nodes =
                        [
                            new FilterCondition
                            {
                                PropertyName = nameof(DataSetVariationDomainConfiguration.OwnerWorkspaceId),
                                Operator = FilterOperators.ByName("Equal"),
                                Value = workspaceId,
                            },
                            new FilterCondition
                            {
                                PropertyName = nameof(DataSetVariationDomainConfiguration.VariationName),
                                Operator = FilterOperators.ByName("Equal"),
                                Value = variationName,
                            },
                        ],
                    },
                },
                null, 0, 1, false),
            ct).ConfigureAwait(false);
        if (page.IsFailure) return page.ToNewResult<DataSetVariationDomainConfiguration?>();

        await foreach (var row in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
            return GenericResult<DataSetVariationDomainConfiguration?>.Success(row);

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<DataSetVariationDomainConfiguration?>()
            : GenericResult<DataSetVariationDomainConfiguration?>.Success(null);
    }

    private static async Task<IGenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>> Read(
        IDataSetVariationConfigurationProvider variations, DomainQuery query, CancellationToken ct)
    {
        var page = await variations.Get(query, ct).ConfigureAwait(false);
        if (page.IsFailure) return page.ToNewResult<IReadOnlyList<DataSetVariationDomainConfiguration>>();

        var rows = new List<DataSetVariationDomainConfiguration>();
        await foreach (var row in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
            rows.Add(row);

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<IReadOnlyList<DataSetVariationDomainConfiguration>>()
            : GenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>.Success(rows);
    }
}
