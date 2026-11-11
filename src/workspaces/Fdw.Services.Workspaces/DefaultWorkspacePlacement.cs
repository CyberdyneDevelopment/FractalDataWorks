using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Commands;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Places a new data set, data store, pipeline or calculation in the default workspace of its owner's default organization (FDW-3692): one
/// <c>Owns</c> resource row, written in the unit of work that makes the thing, so nothing is made that sits in no workspace.
/// </summary>
/// <remarks>
/// The owner of a thing is the tenant the caller is acting in. The tenant's default organization names its default workspace
/// (<c>DefaultWorkspaceId</c>), and the thing goes there. Nothing here guesses: a caller in no tenant, a tenant without a default
/// organization, a default workspace that is not there and a kind that is not a workspace resource kind each fail with a code that says
/// so, and the failure takes the thing with it. With the setting <c>FoundingMakesDefaultWorkspace</c> off (FDW-4600) no organization has a
/// default workspace to name: nothing is placed and nothing is refused for the lack of one (a kind that is not a workspace resource kind
/// is still refused, as it is the request's own fault). The default organization is read from <c>tenant.Organization</c> in the same unit of work,
/// not through <c>IOrganizationProvider</c>: which provider a host registers follows its multitenancy option, and a single-tenant host (the ETL
/// host and the scheduler) registers one that knows no organization, so a placement that asked it would refuse every create on those hosts. The
/// row is inserted under the workspace's current configuration row without re-saving the workspace, so making a thousand data sets does not
/// rewrite the workspace a thousand times.
/// </remarks>
public sealed class DefaultWorkspacePlacement : IConfigurationPlacement
{
    private const string OrganizationContainer = "Organization";
    private const string ConfigurationContainer = "WorkspaceConfiguration";
    private const string ResourceContainer = "WorkspaceResource";
    private const string OwnsRelationship = "Owns";

    private readonly IAuthenticationContextAccessor _authentication;
    private readonly IWorkspaceFoundingPolicy _policy;
    private readonly string _dataStoreName;
    private readonly string _pathName;
    private readonly string _organizationPathName;
    private readonly ILogger<DefaultWorkspacePlacement> _logger;

    /// <summary>Initializes a new instance of the <see cref="DefaultWorkspacePlacement"/> class.</summary>
    /// <param name="authentication">The accessor of the caller's authentication context: the tenant it acts in and the user it is.</param>
    /// <param name="policy">Says whether founding makes a default workspace; when it does not, nothing is placed.</param>
    /// <param name="dataStoreName">The configuration connection the workspace and organization rows live on.</param>
    /// <param name="pathName">The schema the workspace rows live in.</param>
    /// <param name="organizationPathName">The schema the organization rows live in.</param>
    /// <param name="logger">The logger.</param>
    public DefaultWorkspacePlacement(
        IAuthenticationContextAccessor authentication,
        IWorkspaceFoundingPolicy policy,
        string dataStoreName,
        string pathName,
        string organizationPathName,
        ILogger<DefaultWorkspacePlacement>? logger = null)
    {
        _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        _organizationPathName = organizationPathName ?? throw new ArgumentNullException(nameof(organizationPathName));
        _logger = logger ?? NullLogger<DefaultWorkspacePlacement>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Place(PlacementRequest request, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(unit);

        if (ReferenceEquals(WorkspaceResourceKinds.ByName(request.Kind), WorkspaceResourceKinds.NotFound))
            return UnknownKind(request);

        var makesWorkspace = _policy.FoundingMakesDefaultWorkspace();
        if (makesWorkspace.IsFailure) return makesWorkspace;

        if (!makesWorkspace.Value)
        {
            DefaultWorkspaceLog.NotPlaced(_logger, request.Name, request.Kind);
            return GenericResult.Success();
        }

        var caller = Caller(request);
        if (caller.IsFailure) return caller;

        var organization = await DefaultOrganization(request, caller.Value.TenantId, unit, cancellationToken).ConfigureAwait(false);
        if (organization.IsFailure || organization.Value is not { } defaultOrganization) return organization;

        // Setting on: every organization names its default workspace, and one that names none is a failure that says so, as before.
        if (defaultOrganization.DefaultWorkspaceId is not { } workspaceId || workspaceId == Guid.Empty)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("DefaultWorkspaceMissing"), _logger,
                ResultDetails.Create("name", request.Name, "organization", defaultOrganization.Name, "reason", "it names no default workspace"));
        }

        var parent = await ParentKeys(request, defaultOrganization, workspaceId, unit, cancellationToken).ConfigureAwait(false);
        if (parent.IsFailure || parent.Value is not { } foreignKey) return parent;

        DefaultWorkspaceLog.Placing(_logger, request.Name, request.Kind, workspaceId, defaultOrganization.Id);
        return await Insert(request, caller.Value.UserId, foreignKey, unit, cancellationToken).ConfigureAwait(false);
    }

    // The tenant the caller acts in and the user it is, which the placement records as the one who placed the thing.
    private IGenericResult<(Guid TenantId, Guid UserId)> Caller(PlacementRequest request)
    {
        if (_authentication.Current is not { ActiveTenantId: { } tenantId } caller)
        {
            return GenericResult<(Guid TenantId, Guid UserId)>.Failure(
                WorkspacesResultCodes.ByName("PlacementTenantUnknown"), _logger, ResultDetails.Create("name", request.Name));
        }

        return Guid.TryParse(caller.UserId, out var userId)
            ? GenericResult<(Guid TenantId, Guid UserId)>.Success((tenantId, userId))
            : GenericResult<(Guid TenantId, Guid UserId)>.Failure(
                WorkspacesResultCodes.ByName("PlacementUserUnknown"), _logger, ResultDetails.Create("name", request.Name));
    }

    // The tenant's default organization, which names the default workspace. The organization is a real row the tenant's founding made.
    private async Task<IGenericResult<DefaultOrganizationRow>> DefaultOrganization(
        PlacementRequest request, Guid tenantId, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        var query = new QueryCommandBuilder<DefaultOrganizationRow>(_dataStoreName, _organizationPathName, OrganizationContainer)
            .Where("TenantId", tenantId)
            .Where("IsDefault", true)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build().Command;
        var read = await unit.Read<IEnumerable<DefaultOrganizationRow>>(
            query, new DataStoreTarget(_dataStoreName, _organizationPathName, OrganizationContainer), cancellationToken).ConfigureAwait(false);
        if (read.IsFailure) return read.ToNewResult<DefaultOrganizationRow>();

        if (read.Value?.FirstOrDefault() is not { } organization)
        {
            return GenericResult<DefaultOrganizationRow>.Failure(
                WorkspacesResultCodes.ByName("DefaultOrganizationMissing"), _logger,
                ResultDetails.Create("name", request.Name, "reason", "the tenant has no default organization"));
        }

        return GenericResult<DefaultOrganizationRow>.Success(organization);
    }

    // The key values of the default workspace's current configuration row, read in the unit, then the foreign key the resource container
    // declares to it, filled from them.
    private async Task<IGenericResult<IReadOnlyDictionary<string, object?>>> ParentKeys(
        PlacementRequest request, DefaultOrganizationRow organization, Guid workspaceId, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        var container = unit.Container(_pathName, ConfigurationContainer);
        if (container.IsFailure || container.Value is not { } parentContainer) return container.ToNewResult<IReadOnlyDictionary<string, object?>>();

        var configuration = new ImplementationConfigurationCommand<WorkspaceImplementationConfiguration>(ConfigurationContainer);
        var rows = await unit.Read<IEnumerable<Dictionary<string, object?>>>(
            configuration.Get(_dataStoreName, _pathName, workspaceId),
            new DataStoreTarget(_dataStoreName, _pathName, ConfigurationContainer), cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure) return rows.ToNewResult<IReadOnlyDictionary<string, object?>>();

        if (rows.Value?.FirstOrDefault() is not { } current)
        {
            return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                WorkspacesResultCodes.ByName("DefaultWorkspaceMissing"), _logger,
                ResultDetails.Create("name", request.Name, "organization", organization.Name, "reason", workspaceId.ToString("D")));
        }

        return unit.ForeignKeyValues(parentContainer, ResourceContainer, current);
    }

    // One Owns row under the default workspace's current configuration row. It is inserted, never saved over a current row, so the workspace
    // keeps its version and every resource it already holds.
    private async Task<IGenericResult> Insert(
        PlacementRequest request, Guid userId, IReadOnlyDictionary<string, object?> foreignKey, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        var resource = new WorkspaceResourceConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            ResourceType = request.Kind,
            ResourceId = request.Id,
            Relationship = OwnsRelationship,
            AddedByUserId = userId,
            // Stamped here for the reason the attach endpoint stamps it: the insert writes every mapped column, so an unset value would
            // overwrite the column default (FDW-793).
            CreateDate = DateTimeOffset.UtcNow,
        };

        return await unit.Write(
            new WorkspaceResourceConfigurationCommand().Insert(_dataStoreName, _pathName, resource, foreignKey),
            new DataStoreTarget(_dataStoreName, _pathName, ResourceContainer),
            cancellationToken).ConfigureAwait(false);
    }

    private IGenericResult UnknownKind(PlacementRequest request) =>
        GenericResult.Failure(
            WorkspacesResultCodes.ByName("PlacementKindUnknown"), _logger,
            ResultDetails.Create(
                "name", request.Name,
                "kind", request.Kind,
                "kinds", string.Join(", ", WorkspaceResourceKinds.All().Select(kind => kind.Name).OrderBy(name => name, StringComparer.Ordinal))));
}
