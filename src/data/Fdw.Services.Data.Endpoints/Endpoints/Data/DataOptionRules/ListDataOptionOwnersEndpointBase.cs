using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Options;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Logging;
using Fdw.Services.Multitenancy;
using Fdw.Services.Workspaces;
using Fdw.Web.Endpoints.Contracts;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /data-option-owners/search</c> (datastores:write), FDW-6339. Lists a page of the organizations or workspaces, of the kind the query names, that the caller may own a rule in.
/// Built by FDW-6489: the organizations the organization provider lists, or the workspaces the workspace access policy lets the caller view; each is named by the names directory, so a
/// choice with no name fails the list and is never shown empty. A kind left out is refused: nothing picks one.
/// </summary>
public abstract class ListDataOptionOwnersEndpointBase : DataOptionRuleServiceEndpointBase<ListDataOptionOwnersRequest, PaginatedResponse<DataOptionOwnerChoicePayload>>
{
    private readonly IDataConfigurationRuleNames _names;
    private readonly IOrganizationConfigurationProvider _organizations;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _workspaceAccess;

    /// <summary>Initializes a new instance of the <see cref="ListDataOptionOwnersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="names">The names directory.</param>
    /// <param name="organizations">Lists the organizations.</param>
    /// <param name="workspaces">Lists the workspaces.</param>
    /// <param name="workspaceAccess">Whether the caller may view a workspace.</param>
    protected ListDataOptionOwnersEndpointBase(
        ILogger? logger,
        IDataConfigurationRuleNames names,
        IOrganizationConfigurationProvider organizations,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy workspaceAccess)
        : base(logger)
    {
        _names = names;
        _organizations = organizations;
        _workspaces = workspaces;
        _workspaceAccess = workspaceAccess;
    }

    /// <inheritdoc />
    protected override string RouteName => "ListDataOptionOwners";

    /// <inheritdoc />
    protected override async Task<IGenericResult<PaginatedResponse<DataOptionOwnerChoicePayload>>> Run(ListDataOptionOwnersRequest request, CancellationToken cancellationToken)
    {
        if (request.Kind is not { } kind)
            return GenericResult<PaginatedResponse<DataOptionOwnerChoicePayload>>.Failure(DataOptionRulesEndpointLog.RuleFieldMissing(EndpointLogger, RouteName, "kind"));

        var window = Window(request.Page, request.PageSize);
        if (!window.IsSuccess || window.Value is null)
            return window.ToNewResult<PaginatedResponse<DataOptionOwnerChoicePayload>>();

        var ids = await ChoiceIds(kind, cancellationToken).ConfigureAwait(false);
        if (!ids.IsSuccess || ids.Value is null)
            return ids.ToNewResult<PaginatedResponse<DataOptionOwnerChoicePayload>>();

        var named = await _names.Owners(kind, ids.Value, cancellationToken).ConfigureAwait(false);
        if (!named.IsSuccess || named.Value is null)
            return named.ToNewResult<PaginatedResponse<DataOptionOwnerChoicePayload>>();

        var matching = named.Value
            .Where(choice => string.IsNullOrEmpty(request.Search) || choice.Value.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(choice => choice.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(choice => choice.Key)
            .Select(choice => new DataOptionOwnerChoicePayload { Id = choice.Key, Name = choice.Value })
            .ToList();
        return GenericResult<PaginatedResponse<DataOptionOwnerChoicePayload>>.Success(
            PaginatedResponse<DataOptionOwnerChoicePayload>.Create([.. matching.Skip(window.Value.Skip).Take(window.Value.Take)], window.Value.Page, window.Value.Take, matching.Count));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data-option-owners/search");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "List who may own a rule";
            s.Description = "Lists a page of the organizations or workspaces, of the kind the query names, that the caller may own a rule in.";
        });
        ConfigureEndpoint();
    }

    // The ids of the owners of one kind the caller may choose: every organization listed, or the workspaces the caller may view. A kind that is neither is refused with its own code.
    private async Task<IGenericResult<IReadOnlyList<Guid>>> ChoiceIds(string kind, CancellationToken cancellationToken)
    {
        if (string.Equals(kind, DataOptionOwnerKinds.Workspace.Name, StringComparison.Ordinal))
            return await ViewableWorkspaceIds(cancellationToken).ConfigureAwait(false);

        if (!string.Equals(kind, DataOptionOwnerKinds.Organization.Name, StringComparison.Ordinal))
            return GenericResult<IReadOnlyList<Guid>>.Failure(
                DataOptionRulesLog.OwnerKindNotFound(EndpointLogger, kind, string.Join(", ", DataOptionOwnerKinds.All().Select(known => known.Name))));

        var organizations = await _organizations.Get(cancellationToken).ConfigureAwait(false);
        return organizations.IsSuccess && organizations.Value is { } listed
            ? GenericResult<IReadOnlyList<Guid>>.Success([.. listed.Select(organization => organization.Id)])
            : organizations.ToNewResult<IReadOnlyList<Guid>>();
    }

    private async Task<IGenericResult<IReadOnlyList<Guid>>> ViewableWorkspaceIds(CancellationToken cancellationToken)
    {
        var workspaces = await _workspaces.Get(cancellationToken).ConfigureAwait(false);
        if (!workspaces.IsSuccess || workspaces.Value is null)
            return workspaces.ToNewResult<IReadOnlyList<Guid>>();

        var ids = new List<Guid>();
        foreach (var workspace in workspaces.Value)
        {
            var viewable = await _workspaceAccess.MayView(workspace, cancellationToken).ConfigureAwait(false);
            if (viewable.IsSuccess)
                ids.Add(workspace.Id);
            else if (WorkspaceAccessRefusals.IsUndetermined(viewable))
                return viewable.ToNewResult<IReadOnlyList<Guid>>();
        }

        return GenericResult<IReadOnlyList<Guid>>.Success(ids);
    }
}
