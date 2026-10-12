using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Lists the datum types -- what a column's data is -- from the <c>DatumTypes</c> type collection, with
/// what each one means for a column (FDW-988).
/// </summary>
public abstract class ListDatumTypesEndpointBase : CrudListEndpointBase<DatumTypeResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListDatumTypesEndpointBase"/> class.</summary>
    protected ListDatumTypesEndpointBase(ILogger<ListDatumTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "datum-types";

    /// <inheritdoc />
    /// <remarks>
    /// Any signed-in caller may list the types: they are the platform's vocabulary, the same for every
    /// caller, and anyone who types a column has to be able to name one. No <c>datum-types</c> permission
    /// exists to gate them with.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List datum types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns every datum type, NotYetTyped first, with the DatumTypeId to send when typing a field and what the type means for a column.";

    /// <summary>Loads the types from the DatumTypes type collection.</summary>
    protected override Task<IGenericResult<List<DatumTypeResponse>>> LoadItems(CancellationToken ct)
    {
        var items = DatumTypes.All()
            .OrderBy(type => type.Id != DatumTypes.NotYetTyped.Id)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => new DatumTypeResponse
            {
                DatumTypeId = type.Id,
                Name = type.Name,
                Description = type.Description,
                IsDefaultResult = type.IsDefaultResult,
                IsDisplayable = type.IsDisplayable,
                IsKeyRole = type.IsKeyRole,
                IsOrderable = type.IsOrderable,
                IsGroupable = type.IsGroupable,
                IsAggregatable = type.IsAggregatable,
                IsPointInTime = type.IsPointInTime,
                IsLowerDateBoundary = type.IsLowerDateBoundary,
                IsUpperDateBoundary = type.IsUpperDateBoundary,
                Axes = type.Axes.ToList(),
                Aggregates = type.Aggregates.ToList(),
                AllowedOperators = type.AllowedOperators.ToList(),
            })
            .ToList();

        return Task.FromResult(GenericResult<List<DatumTypeResponse>>.Success(items));
    }
}
