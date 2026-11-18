using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.Endpoints.Contracts;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/search</c> (datastores:read), FDW-6339. Lists a page of the container's data option rules, current or removed, for one column or all, for one owner or all.
/// Built by FDW-6489: it answers through the rule service, with the total the service counted.
/// </summary>
public abstract class ListDataOptionRulesEndpointBase : DataOptionRuleServiceEndpointBase<ListDataOptionRulesRequest, PaginatedResponse<DataOptionRulePayload>>
{
    private readonly IDataConfigurationRules _rules;
    private readonly IDataConfigurationRuleNames _names;

    /// <summary>Initializes a new instance of the <see cref="ListDataOptionRulesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected ListDataOptionRulesEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger)
    {
        _rules = rules;
        _names = names;
    }

    /// <inheritdoc />
    protected override string RouteName => "ListDataOptionRules";

    /// <inheritdoc />
    protected override async Task<IGenericResult<PaginatedResponse<DataOptionRulePayload>>> Run(ListDataOptionRulesRequest request, CancellationToken cancellationToken)
    {
        var window = Window(request.Page, request.PageSize);
        if (!window.IsSuccess || window.Value is null)
            return window.ToNewResult<PaginatedResponse<DataOptionRulePayload>>();

        var page = await _rules.List(
            new DataConfigurationRulesQuery
            {
                Target = DataOptionRuleMapping.Target(request),
                Column = request.Column,
                Removed = request.Removed,
                OwnerKind = request.OwnerKind,
                OwnerId = request.OwnerId,
                Skip = window.Value.Skip,
                Take = window.Value.Take,
            },
            cancellationToken).ConfigureAwait(false);
        if (!page.IsSuccess || page.Value is null)
            return page.ToNewResult<PaginatedResponse<DataOptionRulePayload>>();

        var payloads = await DataOptionRuleMapping.Payloads(page.Value.Items, _names, cancellationToken).ConfigureAwait(false);
        return payloads.IsSuccess && payloads.Value is { } items
            ? GenericResult<PaginatedResponse<DataOptionRulePayload>>.Success(PaginatedResponse<DataOptionRulePayload>.Create(items, window.Value.Page, window.Value.Take, page.Value.TotalCount))
            : payloads.ToNewResult<PaginatedResponse<DataOptionRulePayload>>();
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/search");
        Policies("datastores:read");
        Summary(s =>
        {
            s.Summary = "List data option rules";
            s.Description = "Lists a page of the container's data option rules, current or removed, for one column or all, for one owner or all.";
        });
        ConfigureEndpoint();
    }
}
