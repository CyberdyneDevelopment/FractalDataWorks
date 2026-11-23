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
using Fdw.Services.Settings;
using Fdw.Services.Workspaces;
using Fdw.Web.Endpoints.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /data-option-owners/search</c> (datastores:write), FDW-6339. Lists a page of the organizations or workspaces, of the kind the query names, that the caller may own a rule in.
/// Built by FDW-6489: the organizations the organization provider lists, or the workspaces the workspace access policy lets the caller view; a choice with no name fails the list and is
/// never shown empty. A kind left out is refused: nothing picks one. FDW-6717: an organization is read as a page of the organization domain's records (its list fields live on the
/// domain record, which the composing read of a whole organization cannot carry), narrowed to the caller's tenant (FDW-4828) with the size of the read from the setting DataOptionOwnerReadSize, and is named by its record;
/// a workspace is named by the names directory.
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

        var named = await Choices(kind, cancellationToken).ConfigureAwait(false);
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

    // The owners of one kind the caller may choose, each with its name. A kind that is neither organization nor workspace is refused with its own code.
    private async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Choices(string kind, CancellationToken cancellationToken)
    {
        if (string.Equals(kind, DataOptionOwnerKinds.Organization.Name, StringComparison.Ordinal))
            return await OrganizationChoices(cancellationToken).ConfigureAwait(false);

        if (!string.Equals(kind, DataOptionOwnerKinds.Workspace.Name, StringComparison.Ordinal))
            return GenericResult<IReadOnlyDictionary<Guid, string>>.Failure(
                DataOptionRulesLog.OwnerKindNotFound(EndpointLogger, kind, string.Join(", ", DataOptionOwnerKinds.All().Select(known => known.Name))));

        var ids = await ViewableWorkspaceIds(cancellationToken).ConfigureAwait(false);
        return ids.IsSuccess && ids.Value is { } viewable
            ? await _names.Owners(kind, viewable, cancellationToken).ConfigureAwait(false)
            : ids.ToNewResult<IReadOnlyDictionary<Guid, string>>();
    }

    // The organization domain keeps its fields (tenant, slug, flags) on its domain record and its implementation carries none of them, so the composing read of a whole organization can
    // never answer for it (ListFieldNotOnImplementation); a page of domain records is the read the domain serves (FDW-4233). The tenant schema has no row-level security, so the read is
    // narrowed to the caller's tenant by the one mechanism every organization list uses, TenantOrganizations (FDW-4828), with the read size from the setting DataOptionOwnerReadSize
    // (FDW-6717); a tenant with more organizations than the size fails, because a list that silently dropped some would hide where a rule may be owned.
    private async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> OrganizationChoices(CancellationToken cancellationToken)
    {
        var tenant = TenantOrganizations.CallerTenant(User, EndpointLogger, RouteName);
        if (!tenant.IsSuccess)
            return tenant.ToNewResult<IReadOnlyDictionary<Guid, string>>();

        var organizations = await _organizations
            .OfTenant(tenant.Value, HttpContext.RequestServices.GetRequiredService<IEffectiveSettingsProvider>(), SettingDefinitions.DataOptionOwnerReadSize, EndpointLogger, cancellationToken)
            .ConfigureAwait(false);
        if (!organizations.IsSuccess || organizations.Value is not { } rows)
            return organizations.ToNewResult<IReadOnlyDictionary<Guid, string>>();

        var named = new Dictionary<Guid, string>();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
                return GenericResult<IReadOnlyDictionary<Guid, string>>.Failure(
                    DataOptionRulesLog.OwnerNotFound(EndpointLogger, DataOptionOwnerKinds.Organization.Name, row.Id.ToString("D")));

            named[row.Id] = row.Name;
        }

        return GenericResult<IReadOnlyDictionary<Guid, string>>.Success(named);
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
