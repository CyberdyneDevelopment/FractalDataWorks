using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Lists the stand-in strategies a data set field can be given, with their parameter schemas.</summary>
public abstract class ListStandInStrategiesEndpointBase : CrudListEndpointBase<StandInStrategyDto>
{
    /// <inheritdoc />
    protected ListStandInStrategiesEndpointBase(ILogger<ListStandInStrategiesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "standin-strategies";

    /// <summary>Gated by "datasets:read" — strategies are a sub-resource of the DataSets domain and
    /// "standin-strategies:read" was never seeded, confirmed live (403).</summary>
    protected override string ReadPolicy => "datasets:read";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<StandInStrategyDto>>> LoadItems(CancellationToken ct)
    {
        var strategies = StandInStrategies.All()
            .Select(s => new StandInStrategyDto
            {
                Name = s.Name,
                Parameters = s.Parameters,
                Limitations = s.Limitations,
            })
            .ToList();

        return Task.FromResult(GenericResult<List<StandInStrategyDto>>.Success(strategies));
    }
}
