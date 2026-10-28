using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Configuration;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// The connections assigned to organizations, kept as managed configuration (FDW-1581).
/// </summary>
/// <remarks>
/// An assignment is saved under the name <c>{OrganizationId}:{Purpose}</c>. The organization must be one the caller may administer
/// (<see cref="IOrganizationScope"/>), the grant must be one the organization's own tenant holds, and the base folder is placed inside
/// the grant's folder when the store is resolved, so it cannot leave it.
/// </remarks>
public sealed class ConnectionAssignments : IConnectionAssignments
{
    private const string AssignmentDomain = "ConnectionAssignment";

    private readonly IOrganizationScope _scope;
    private readonly IConnectionAssignmentConfigurationProvider _assignments;
    private readonly IConnectionGrantConfigurationProvider _grants;
    private readonly ILogger<ConnectionAssignments> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConnectionAssignments"/> class.</summary>
    /// <param name="scope">Judges whether the caller may administer the organization and names its tenant.</param>
    /// <param name="assignments">Reads and writes the assignments.</param>
    /// <param name="grants">Reads the grants.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionAssignments(
        IOrganizationScope scope,
        IConnectionAssignmentConfigurationProvider assignments,
        IConnectionGrantConfigurationProvider grants,
        ILogger<ConnectionAssignments>? logger)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _logger = logger ?? NullLogger<ConnectionAssignments>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ConnectionAssignmentView>> Assign(
        Guid organizationId, string purpose, Guid grantId, string? baseFolder, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(OrganizationRights.Write, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<ConnectionAssignmentView>();
        }

        var parsedFolder = DocumentPath.Parse(baseFolder);
        if (parsedFolder.IsFailure)
        {
            return GenericResult<ConnectionAssignmentView>.Failure(ConnectionGrantsLog.FolderInvalid(_logger, "base", baseFolder, parsedFolder.CurrentMessage));
        }

        var grant = await CheckGrant(organizationId, tenant.Value, purpose, grantId, cancellationToken).ConfigureAwait(false);
        if (grant.IsFailure || grant.Value is null)
        {
            return grant.ToNewResult<ConnectionAssignmentView>();
        }

        var existing = await Find(organizationId, purpose, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return existing.ToNewResult<ConnectionAssignmentView>();
        }

        // Why an existing assignment keeps its stored name: the purpose is compared without regard to case, and a second spelling would
        // otherwise be a second row under the unique name.
        var name = existing.Value is { } current ? current.Name : NameOf(organizationId, purpose);
        var parsedBase = parsedFolder.Value.Value;
        var saved = await _assignments.Save(
            new ConnectionAssignmentImplementationConfiguration
            {
                Name = name,
                OrganizationId = organizationId,
                Purpose = purpose,
                GrantId = grantId,
                BaseFolder = parsedBase.Length == 0 ? null : parsedBase,
            },
            AssignmentDomain, AssignmentDomain, name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return GenericResult<ConnectionAssignmentView>.Failure(ConnectionGrantsLog.WriteFailed(_logger, $"The assignment '{name}'", saved.CurrentMessage));
        }

        ConnectionGrantsLog.Assigned(_logger, grantId, organizationId, purpose, parsedBase);
        return GenericResult<ConnectionAssignmentView>.Success(
            new ConnectionAssignmentView(organizationId, purpose, grantId, grant.Value.ConnectionName, grant.Value.FolderPath, parsedBase.Length == 0 ? null : parsedBase));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ConnectionAssignmentView>> Unassign(Guid organizationId, string purpose, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(OrganizationRights.Write, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<ConnectionAssignmentView>();
        }

        var existing = await Find(organizationId, purpose, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure || existing.Value is not { } assignment)
        {
            return existing.IsFailure
                ? existing.ToNewResult<ConnectionAssignmentView>()
                : GenericResult<ConnectionAssignmentView>.Failure(ConnectionGrantsLog.AssignmentNotFound(_logger, organizationId, purpose));
        }

        var removed = await _assignments.Delete(assignment.Name, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return GenericResult<ConnectionAssignmentView>.Failure(ConnectionGrantsLog.WriteFailed(_logger, "The assignment", removed.CurrentMessage));
        }

        ConnectionGrantsLog.Unassigned(_logger, purpose, organizationId);
        return GenericResult<ConnectionAssignmentView>.Success(await ViewOf(assignment, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<ConnectionAssignmentView>>> List(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(OrganizationRights.Read, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<IReadOnlyList<ConnectionAssignmentView>>();
        }

        var all = await _assignments.Get(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
        {
            return GenericResult<IReadOnlyList<ConnectionAssignmentView>>.Failure(
                ConnectionGrantsLog.ReadFailed(_logger, $"The assignments of organization '{organizationId}'", all.CurrentMessage));
        }

        var views = new List<ConnectionAssignmentView>();
        foreach (var assignment in all.Value.Where(row => row.OrganizationId == organizationId && row.IsCurrent && !row.IsDeleted))
        {
            views.Add(await ViewOf(assignment, cancellationToken).ConfigureAwait(false));
        }

        return GenericResult<IReadOnlyList<ConnectionAssignmentView>>.Success(views);
    }

    private static string NameOf(Guid organizationId, string purpose) => $"{organizationId}:{purpose}";

    // The checks every assignment makes first: a purpose, a grant that exists, is the organization's tenant's and is for the purpose. The result carries the grant.
    private async Task<IGenericResult<IConnectionGrantImplementationConfiguration>> CheckGrant(
        Guid organizationId, Guid tenantId, string purpose, Guid grantId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(purpose))
        {
            return GenericResult<IConnectionGrantImplementationConfiguration>.Failure(ConnectionGrantsLog.FieldRequired(_logger, "purpose"));
        }

        var grant = await _grants.Get(grantId, cancellationToken).ConfigureAwait(false);
        if (grant.IsFailure || grant.Value is null)
        {
            return GenericResult<IConnectionGrantImplementationConfiguration>.Failure(ConnectionGrantsLog.GrantIdNotFound(_logger, grantId));
        }

        if (grant.Value.GranteeTenantId != tenantId)
        {
            return GenericResult<IConnectionGrantImplementationConfiguration>.Failure(ConnectionGrantsLog.NotTheTenantsGrant(_logger, grantId, organizationId));
        }

        return string.Equals(grant.Value.Purpose, purpose, StringComparison.OrdinalIgnoreCase)
            ? grant
            : GenericResult<IConnectionGrantImplementationConfiguration>.Failure(ConnectionGrantsLog.PurposeDiffers(_logger, grantId, grant.Value.Purpose, purpose));
    }

    // The live assignment of an organization for a purpose; Success with a null value when there is none.
    private async Task<IGenericResult<IConnectionAssignmentImplementationConfiguration?>> Find(Guid organizationId, string purpose, CancellationToken cancellationToken)
    {
        var all = await _assignments.Get(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
        {
            return GenericResult<IConnectionAssignmentImplementationConfiguration?>.Failure(
                ConnectionGrantsLog.ReadFailed(_logger, $"The assignments of organization '{organizationId}'", all.CurrentMessage));
        }

        return GenericResult<IConnectionAssignmentImplementationConfiguration?>.Success(
            all.Value.FirstOrDefault(row => row.OrganizationId == organizationId && row.IsCurrent && !row.IsDeleted
                && string.Equals(row.Purpose, purpose, StringComparison.OrdinalIgnoreCase)));
    }

    // The assignment with the connection and folder of its grant; when the grant is gone the connection and folder are not stated.
    private async Task<ConnectionAssignmentView> ViewOf(IConnectionAssignmentImplementationConfiguration assignment, CancellationToken cancellationToken)
    {
        var grant = await _grants.Get(assignment.GrantId, cancellationToken).ConfigureAwait(false);
        return grant.IsSuccess && grant.Value is { } found
            ? new ConnectionAssignmentView(assignment.OrganizationId, assignment.Purpose, assignment.GrantId, found.ConnectionName, found.FolderPath, assignment.BaseFolder)
            : new ConnectionAssignmentView(assignment.OrganizationId, assignment.Purpose, assignment.GrantId, null, null, assignment.BaseFolder);
    }
}
