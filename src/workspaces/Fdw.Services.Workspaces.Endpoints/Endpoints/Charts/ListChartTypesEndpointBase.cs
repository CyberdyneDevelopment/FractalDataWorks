using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Workspaces.Charts;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Lists the chart types a view can be drawn as, from the <c>ChartTypes</c> type collection, with the slots each one takes and
/// the axes each slot accepts (FDW-1032).
/// </summary>
/// <remarks>
/// The data view workbench builds its chart-type picker and its slot pickers from this, with the axes and aggregates of
/// <c>GET /datum-types</c>, so nothing about which field fits which slot is written into a screen. A chart type added to the
/// collection appears here without a change to any client.
/// </remarks>
public abstract class ListChartTypesEndpointBase : CrudListEndpointBase<ChartTypeResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListChartTypesEndpointBase"/> class.</summary>
    protected ListChartTypesEndpointBase(ILogger<ListChartTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "chart-types";

    /// <inheritdoc />
    /// <remarks>
    /// Any signed-in caller may list the types: they are the platform's vocabulary, the same for every caller, and anyone who
    /// builds a view has to be able to name one. No <c>chart-types</c> permission exists to gate them with.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List chart types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns every chart type a data view can be drawn as, by name, with the ways of binding fields to it: each shape's slots, " +
        "whether a slot is required or takes several fields, and the axes it accepts.";

    /// <summary>Loads the chart types from the ChartTypes type collection.</summary>
    protected override Task<IGenericResult<List<ChartTypeResponse>>> LoadItems(CancellationToken ct)
    {
        var items = ChartTypes.All()
            .Where(type => !ReferenceEquals(type, ChartTypes.NotFound))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => new ChartTypeResponse
            {
                Name = type.Name,
                Description = type.Description,
                Shapes = type.Shapes.Select(shape => new ChartShapeResponse
                {
                    Name = shape.Name,
                    Slots = shape.Slots.Select(slot => new ChartSlotResponse
                    {
                        Name = slot.Name,
                        Axes = slot.Axes.ToList(),
                        IsRequired = slot.IsRequired,
                        AllowsMany = slot.AllowsMany,
                    }).ToList(),
                }).ToList(),
            })
            .ToList();

        return Task.FromResult(GenericResult<List<ChartTypeResponse>>.Success(items));
    }
}
