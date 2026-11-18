using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.Endpoints.Contracts;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /data-option-custodians/search</c> (datastores:write), FDW-6339. Lists a page of the people who hold datastores:write, who may be a data option rule's custodian.
/// Built by FDW-6489: the holders and their names come from the names directory; the search keeps the names that contain the text, without regard to case.
/// </summary>
public abstract class ListDataOptionCustodiansEndpointBase : DataOptionRuleServiceEndpointBase<ListDataOptionCustodiansRequest, PaginatedResponse<DataOptionCustodianPayload>>
{
    private readonly IDataConfigurationRuleNames _names;

    /// <summary>Initializes a new instance of the <see cref="ListDataOptionCustodiansEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="names">The names directory.</param>
    protected ListDataOptionCustodiansEndpointBase(ILogger? logger, IDataConfigurationRuleNames names)
        : base(logger)
    {
        _names = names;
    }

    /// <inheritdoc />
    protected override string RouteName => "ListDataOptionCustodians";

    /// <inheritdoc />
    protected override async Task<IGenericResult<PaginatedResponse<DataOptionCustodianPayload>>> Run(ListDataOptionCustodiansRequest request, CancellationToken cancellationToken)
    {
        var window = Window(request.Page, request.PageSize);
        if (!window.IsSuccess || window.Value is null)
            return window.ToNewResult<PaginatedResponse<DataOptionCustodianPayload>>();

        var holders = await _names.Custodians(cancellationToken).ConfigureAwait(false);
        if (!holders.IsSuccess || holders.Value is null)
            return holders.ToNewResult<PaginatedResponse<DataOptionCustodianPayload>>();

        var matching = holders.Value
            .Where(holder => string.IsNullOrEmpty(request.Search) || holder.Value.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(holder => holder.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(holder => holder.Key)
            .Select(holder => new DataOptionCustodianPayload { UserId = holder.Key, Name = holder.Value })
            .ToList();
        return GenericResult<PaginatedResponse<DataOptionCustodianPayload>>.Success(
            PaginatedResponse<DataOptionCustodianPayload>.Create([.. matching.Skip(window.Value.Skip).Take(window.Value.Take)], window.Value.Page, window.Value.Take, matching.Count));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data-option-custodians/search");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "List who may be a custodian";
            s.Description = "Lists a page of the people who hold datastores:write, who may be a data option rule's custodian.";
        });
        ConfigureEndpoint();
    }
}
