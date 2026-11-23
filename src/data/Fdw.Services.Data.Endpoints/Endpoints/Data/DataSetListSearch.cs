using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// What <c>GET /datasets?q=</c> asks of the data set domain (FDW-6119): the text is looked for in the name, the display name and the abbreviation of
/// each data set, which all sit on the domain row, so the search is one query on the domain table, and the ids it finds narrow the list.
/// </summary>
internal static class DataSetListSearch
{
    // Every match is read: the category, data set type and tag selections narrow the same list, and the list is cut to a page once all of them have.
    private const int AllRows = int.MaxValue - 1;

    private static readonly IReadOnlyList<string> TextProperties =
    [
        nameof(DataSetDomainConfiguration.Name),
        nameof(DataSetDomainConfiguration.DisplayName),
        nameof(DataSetDomainConfiguration.Abbreviation),
    ];

    /// <summary>Builds the query that finds the data sets a search keeps.</summary>
    /// <param name="search">The search text, match and identifiers the request asked for.</param>
    /// <returns>The query, in name order and with no total; nothing when the search keeps every data set.</returns>
    internal static DomainQuery? Query(ListSearch search) =>
        search.Filter(TextProperties, nameof(DataSetDomainConfiguration.Id)) is { } filter
            ? new DomainQuery(
                filter,
                new OrderingExpression
                {
                    OrderedFields = [new OrderedField { PropertyName = nameof(DataSetDomainConfiguration.Name), Direction = SortDirections.ByName("Ascending") }],
                },
                0,
                AllRows,
                false)
            : null;

    /// <summary>Finds the ids of the data sets a search keeps, in one query on the domain table.</summary>
    /// <param name="records">Reads the page of domain records the query asks for.</param>
    /// <param name="search">The search text, match and identifiers the request asked for.</param>
    /// <param name="logger">The logger the failure is written to.</param>
    /// <param name="cancellationToken">Ends the search: with the token cancelled it answers <c>ListCancelled</c> and asks no one, or stops reading and answers it.</param>
    /// <returns>The ids found; nothing when the request searches nothing, whatever the token says; or a failure.</returns>
    internal static async Task<IGenericResult<IReadOnlySet<Guid>?>> Found(
        IDataSetRecordProvider records, ListSearch search, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (Query(search) is not { } query)
        {
            return GenericResult<IReadOnlySet<Guid>?>.Success(null);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return GenericResult<IReadOnlySet<Guid>?>.Failure(DataSetsResultCodes.ListCancelled, logger, ResultDetails.Create());
        }

        try
        {
            var page = await records.Get(query, cancellationToken).ConfigureAwait(false);
            if (page.IsFailure || page.Value is not { } read)
            {
                return page.ToNewResult<IReadOnlySet<Guid>?>();
            }

            HashSet<Guid> ids = [];
            await foreach (var record in read.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ids.Add(record.Id);
            }

            return read.Outcome.IsFailure
                ? read.Outcome.ToNewResult<IReadOnlySet<Guid>?>()
                : GenericResult<IReadOnlySet<Guid>?>.Success(ids);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return GenericResult<IReadOnlySet<Guid>?>.Failure(DataSetsResultCodes.ListCancelled, logger, ResultDetails.Create());
        }
    }

    /// <summary>Keeps the data sets the search found.</summary>
    /// <param name="dataSets">The data sets the other selections kept.</param>
    /// <param name="found">The ids the search found; null when the request searched nothing.</param>
    /// <returns>The data sets, in the order they were read.</returns>
    internal static IReadOnlyList<IDataSetImplementationConfiguration> Keep(
        IReadOnlyList<IDataSetImplementationConfiguration> dataSets, IReadOnlySet<Guid>? found) =>
        found is null ? dataSets : [.. dataSets.Where(dataSet => found.Contains(dataSet.Id))];
}
