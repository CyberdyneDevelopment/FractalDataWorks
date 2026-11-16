using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Options;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Lists the data option types and each type's options (<c>GET /data-option-types</c>, FDW-5665), read from the DataOptions
/// type collections, so an option type or option a package adds is offered with no change here or in the console.
/// </summary>
public abstract class ListDataOptionTypesEndpointBase : CrudListEndpointBase<DataOptionTypePayload>
{
    /// <summary>Initializes a new instance of the <see cref="ListDataOptionTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListDataOptionTypesEndpointBase(ILogger<ListDataOptionTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "data-option-types";

    /// <summary>Gets the authorization policy: anyone who can read a data store may see the options a column can take.</summary>
    protected override string ReadPolicy => "datastores:read";

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "List the data option types and their options";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription =>
        "Returns each data option type a row can state as its OptionType, with the options it can state as its Option, in the order they apply.";

    /// <summary>Loads every registered option type, in name order, with its options in the order they apply.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The option types, without the sentinel for a name that did not resolve.</returns>
    protected override Task<IGenericResult<List<DataOptionTypePayload>>> LoadItems(CancellationToken ct)
    {
        DataOptionTypeEndpointLog.ListingDataOptionTypes(Logger);

        var items = DataOptions.All()
            .Where(t => !ReferenceEquals(t, DataOptions.NotFound))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => new DataOptionTypePayload
            {
                Name = t.Name,
                Options = [.. t.Options.OrderBy(o => o.ApplyOrder).ThenBy(o => o.Name, StringComparer.Ordinal).Select(o => o.Name)],
            })
            .ToList();

        DataOptionTypeEndpointLog.ListedDataOptionTypes(Logger, items.Count);
        return Task.FromResult(GenericResult<List<DataOptionTypePayload>>.Success(items));
    }
}
