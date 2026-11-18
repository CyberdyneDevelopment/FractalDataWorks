using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Options;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>
/// The names of users and owners for data configuration rules (FDW-6346): users through the account lookup the audit uses, organizations through
/// the organization provider, workspaces through the workspaces domain's own provider (the one the workspace pages read). An owner that is not found, or whose
/// name is blank, fails with its own code and is never answered as an empty name. A workspace is named by its <c>Name</c>, the field that identifies it; no other
/// field stands in for it.
/// </summary>
public sealed class DataConfigurationRuleNames : IDataConfigurationRuleNames
{
    private const string OrganizationKind = "Organization";
    private const string WorkspaceKind = "Workspace";

    private readonly ILogger<DataConfigurationRuleNames> _logger;
    private readonly IUserNames _users;
    private readonly IOrganizationProvider _organizations;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IPermissionHolders _holders;
    private readonly ITenantContext _tenant;

    /// <summary>Initializes a new instance of the <see cref="DataConfigurationRuleNames"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="users">The account lookup that names a user.</param>
    /// <param name="organizations">The provider that answers an organization by id.</param>
    /// <param name="workspaces">The provider that answers a workspace by id.</param>
    /// <param name="holders">Who holds <c>datastores:write</c>, the people who may be a custodian.</param>
    /// <param name="tenant">The caller's tenant, whose holders the custodian picker lists.</param>
    public DataConfigurationRuleNames(
        ILogger<DataConfigurationRuleNames>? logger,
        IUserNames users,
        IOrganizationProvider organizations,
        IWorkspaceConfigurationProvider workspaces,
        IPermissionHolders holders,
        ITenantContext tenant)
    {
        _logger = logger ?? NullLogger<DataConfigurationRuleNames>.Instance;
        _users = users;
        _organizations = organizations;
        _workspaces = workspaces;
        _holders = holders;
        _tenant = tenant;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Users(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var names = new Dictionary<Guid, string>();
        foreach (var id in userIds.Distinct())
        {
            var found = await _users.Get(id, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess || string.IsNullOrWhiteSpace(found.Value))
                return GenericResult<IReadOnlyDictionary<Guid, string>>.Failure(DataOptionRulesLog.UserNotFound(_logger, id.ToString("D")));

            names[id] = found.Value;
        }

        return GenericResult<IReadOnlyDictionary<Guid, string>>.Success(names);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Custodians(CancellationToken cancellationToken = default)
    {
        var holders = await DataOptionCustodianHolders.Get(_holders, _tenant, cancellationToken).ConfigureAwait(false);
        if (!holders.IsSuccess || holders.Value is null)
            return holders.ToNewResult<IReadOnlyDictionary<Guid, string>>();

        return await Users(holders.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Owners(string ownerKind, IEnumerable<Guid> ownerIds, CancellationToken cancellationToken = default)
    {
        if (ReferenceEquals(DataOptionOwnerKinds.ByName(ownerKind), DataOptionOwnerKinds.NotFound))
            return GenericResult<IReadOnlyDictionary<Guid, string>>.Failure(
                DataOptionRulesLog.OwnerKindNotFound(_logger, ownerKind, string.Join(", ", DataOptionOwnerKinds.All().Select(known => known.Name))));

        var names = new Dictionary<Guid, string>();
        foreach (var id in ownerIds.Distinct())
        {
            var name = await OwnerName(ownerKind, id, cancellationToken).ConfigureAwait(false);
            if (name is null)
                return GenericResult<IReadOnlyDictionary<Guid, string>>.Failure(DataOptionRulesLog.OwnerNotFound(_logger, ownerKind, id.ToString("D")));

            names[id] = name;
        }

        return GenericResult<IReadOnlyDictionary<Guid, string>>.Success(names);
    }

    private Task<string?> OwnerName(string ownerKind, Guid id, CancellationToken cancellationToken)
        => string.Equals(ownerKind, OrganizationKind, StringComparison.Ordinal) ? OrganizationName(id, cancellationToken)
            : string.Equals(ownerKind, WorkspaceKind, StringComparison.Ordinal) ? WorkspaceName(id, cancellationToken)
            : Task.FromResult<string?>(null);

    private async Task<string?> WorkspaceName(Guid id, CancellationToken cancellationToken)
    {
        var found = await _workspaces.Get(id, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess && found.Value is { } workspace && !string.IsNullOrWhiteSpace(workspace.Name) ? workspace.Name : null;
    }

    private async Task<string?> OrganizationName(Guid id, CancellationToken cancellationToken)
    {
        var found = await _organizations.Get(id, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess && found.Value is { } organization && !string.IsNullOrWhiteSpace(organization.Name) ? organization.Name : null;
    }
}
