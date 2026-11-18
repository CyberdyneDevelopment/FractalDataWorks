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
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/history/search</c> (datastores:read), FDW-6339. Lists a page of the recorded changes of the container's rules, newest first:
/// who, when, what kind and each member's old and new value. Built by FDW-6571: the rule service reads the administrative audit for the container's rules and names each record through the catalog of
/// change kinds and members; the route maps the page. A change the catalog does not know fails the page with its own code.
/// </summary>
public abstract class ListDataOptionRuleHistoryEndpointBase : DataOptionRuleServiceEndpointBase<ListDataOptionRuleHistoryRequest, PaginatedResponse<DataOptionRuleChangePayload>>
{
    private readonly IDataConfigurationRules _rules;

    /// <summary>Initializes a new instance of the <see cref="ListDataOptionRuleHistoryEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    protected ListDataOptionRuleHistoryEndpointBase(ILogger? logger, IDataConfigurationRules rules)
        : base(logger)
    {
        _rules = rules;
    }

    /// <inheritdoc />
    protected override string RouteName => "ListDataOptionRuleHistory";

    /// <inheritdoc />
    protected override async Task<IGenericResult<PaginatedResponse<DataOptionRuleChangePayload>>> Run(ListDataOptionRuleHistoryRequest request, CancellationToken cancellationToken)
    {
        var window = Window(request.Page, request.PageSize);
        if (!window.IsSuccess || window.Value is null)
            return window.ToNewResult<PaginatedResponse<DataOptionRuleChangePayload>>();

        var page = await _rules.History(
            new DataConfigurationRuleHistoryQuery
            {
                Target = DataOptionRuleMapping.Target(request),
                Page = window.Value.Page,
                PageSize = window.Value.Take,
                Column = request.Column,
                RuleId = request.RuleId,
            },
            cancellationToken).ConfigureAwait(false);
        return page.IsSuccess && page.Value is { } found
            ? GenericResult<PaginatedResponse<DataOptionRuleChangePayload>>.Success(
                PaginatedResponse<DataOptionRuleChangePayload>.Create([.. found.Items.Select(DataOptionRuleMapping.Change)], window.Value.Page, window.Value.Take, found.TotalCount))
            : page.ToNewResult<PaginatedResponse<DataOptionRuleChangePayload>>();
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/history/search");
        Policies("datastores:read");
        Summary(s =>
        {
            s.Summary = "List the changes of the data option rules";
            s.Description = "Lists a page of the recorded changes of the container's rules, newest first: who, when, what kind and each member's old and new value.";
        });
        ConfigureEndpoint();
    }
}
