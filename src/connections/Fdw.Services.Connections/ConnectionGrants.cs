using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Configuration;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// The connections granted to tenants, kept as managed configuration (FDW-1581).
/// </summary>
/// <remarks>
/// A grant is saved under the name <c>{TenantId}:{Purpose}:{ConnectionName}</c>. Who may grant is settled by the endpoint; this applies
/// the rules that do not depend on the caller.
/// </remarks>
public sealed class ConnectionGrants : IConnectionGrants
{
    private const string GrantDomain = "ConnectionGrant";

    private readonly IConnectionGrantConfigurationProvider _grants;
    private readonly IConnectionAssignmentConfigurationProvider _assignments;
    private readonly IConnectionProvider _connections;
    private readonly ITenantProvider _tenants;
    private readonly ILogger<ConnectionGrants> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConnectionGrants"/> class.</summary>
    /// <param name="grants">Reads and writes the grants.</param>
    /// <param name="assignments">Reads the assignments, to keep a grant that is in use.</param>
    /// <param name="connections">Opens the connection being granted, so the purpose can say whether it fits.</param>
    /// <param name="tenants">Finds the tenant being granted to.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionGrants(
        IConnectionGrantConfigurationProvider grants,
        IConnectionAssignmentConfigurationProvider assignments,
        IConnectionProvider connections,
        ITenantProvider tenants,
        ILogger<ConnectionGrants>? logger)
    {
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _logger = logger ?? NullLogger<ConnectionGrants>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ConnectionGrantView>> Grant(
        Guid tenantId, string connectionName, string purpose, string? folder, bool mayRegisterOwn, CancellationToken cancellationToken = default)
    {
        var checkedInput = await CheckGrantInput(tenantId, connectionName, purpose, folder, cancellationToken).ConfigureAwait(false);
        if (checkedInput.IsFailure)
        {
            return checkedInput.ToNewResult<ConnectionGrantView>();
        }

        var existing = await Find(tenantId, connectionName, purpose, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure || existing.Value is not null)
        {
            return existing.IsFailure
                ? existing.ToNewResult<ConnectionGrantView>()
                : GenericResult<ConnectionGrantView>.Failure(ConnectionGrantsLog.GrantExists(_logger, tenantId, connectionName, purpose));
        }

        var name = NameOf(tenantId, connectionName, purpose);
        var saved = await _grants.Save(
            new ConnectionGrantImplementationConfiguration
            {
                Name = name,
                GranteeTenantId = tenantId,
                ConnectionName = connectionName,
                Purpose = purpose,
                FolderPath = checkedInput.Value.Value.Length == 0 ? null : checkedInput.Value.Value,
                MayRegisterOwn = mayRegisterOwn,
            },
            GrantDomain, GrantDomain, name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return GenericResult<ConnectionGrantView>.Failure(ConnectionGrantsLog.WriteFailed(_logger, $"The grant '{name}'", saved.CurrentMessage));
        }

        ConnectionGrantsLog.Granted(_logger, connectionName, tenantId, purpose, checkedInput.Value.Value);
        var created = await Find(tenantId, connectionName, purpose, cancellationToken).ConfigureAwait(false);
        return created.IsFailure || created.Value is null
            ? GenericResult<ConnectionGrantView>.Failure(ConnectionGrantsLog.ReadFailed(_logger, $"The grant '{name}' just made", created.CurrentMessage))
            : GenericResult<ConnectionGrantView>.Success(ViewOf(created.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ConnectionGrantView>> Revoke(Guid tenantId, string connectionName, string purpose, CancellationToken cancellationToken = default)
    {
        var existing = await Find(tenantId, connectionName, purpose, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure || existing.Value is not { } grant)
        {
            return existing.IsFailure
                ? existing.ToNewResult<ConnectionGrantView>()
                : GenericResult<ConnectionGrantView>.Failure(ConnectionGrantsLog.GrantNotFound(_logger, connectionName, tenantId, purpose));
        }

        // Why a superseded assignment counts: files stored under it are still read through this grant (FDW-3396).
        var assigned = await _assignments.Find<ConnectionAssignmentImplementationConfiguration>(
            assignment => assignment.GrantId == grant.Id && assignment.IsCurrent && !assignment.IsDeleted, cancellationToken).ConfigureAwait(false);
        if (assigned.IsFailure || assigned.Value is null)
        {
            return GenericResult<ConnectionGrantView>.Failure(ConnectionGrantsLog.ReadFailed(_logger, "The assignments of the grant", assigned.CurrentMessage));
        }

        if (assigned.Value.Count > 0)
        {
            return GenericResult<ConnectionGrantView>.Failure(ConnectionGrantsLog.GrantAssigned(_logger, connectionName, tenantId, assigned.Value.Count));
        }

        var removed = await _grants.Delete(NameOf(tenantId, connectionName, purpose), cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return GenericResult<ConnectionGrantView>.Failure(ConnectionGrantsLog.WriteFailed(_logger, "The grant", removed.CurrentMessage));
        }

        ConnectionGrantsLog.Revoked(_logger, connectionName, tenantId, purpose);
        return GenericResult<ConnectionGrantView>.Success(ViewOf(grant));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<ConnectionGrantView>>> List(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var all = await _grants.Get(cancellationToken).ConfigureAwait(false);
        return all.IsSuccess && all.Value is { } grants
            ? GenericResult<IReadOnlyList<ConnectionGrantView>>.Success(
                [.. grants.Where(grant => grant.GranteeTenantId == tenantId && grant.IsCurrent && !grant.IsDeleted).Select(ViewOf)])
            : GenericResult<IReadOnlyList<ConnectionGrantView>>.Failure(ConnectionGrantsLog.ReadFailed(_logger, $"The grants of tenant '{tenantId}'", all.CurrentMessage));
    }

    private static string NameOf(Guid tenantId, string connectionName, string purpose) => $"{tenantId}:{purpose}:{connectionName}";

    private static ConnectionGrantView ViewOf(IConnectionGrantImplementationConfiguration grant)
        => new(grant.Id, grant.GranteeTenantId, grant.ConnectionName, grant.Purpose, grant.FolderPath, grant.MayRegisterOwn);

    // The checks every grant makes first: the texts are given, the purpose is one, the folder is a folder the purpose takes, the tenant exists,
    // and the connection fits the purpose (FDW-3673). The result carries the folder as parsed.
    private async Task<IGenericResult<DocumentPath>> CheckGrantInput(Guid tenantId, string connectionName, string purpose, string? folder, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionName) || string.IsNullOrWhiteSpace(purpose))
        {
            return GenericResult<DocumentPath>.Failure(ConnectionGrantsLog.FieldRequired(_logger, string.IsNullOrWhiteSpace(connectionName) ? "connection name" : "purpose"));
        }

        if (PurposeLookup.Named(purpose) is not { } purposeOption)
        {
            return GenericResult<DocumentPath>.Failure(ConnectionGrantsLog.PurposeUnknown(_logger, purpose, PurposeLookup.Names()));
        }

        if (!purposeOption.TakesFolder && !string.IsNullOrWhiteSpace(folder))
        {
            return GenericResult<DocumentPath>.Failure(ConnectionGrantsLog.FolderNotTaken(_logger, "grant", purpose, folder));
        }

        var parsed = DocumentPath.Parse(folder);
        if (parsed.IsFailure)
        {
            return GenericResult<DocumentPath>.Failure(ConnectionGrantsLog.FolderInvalid(_logger, "grant", folder, parsed.CurrentMessage));
        }

        var tenant = await _tenants.GetTenant(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return GenericResult<DocumentPath>.Failure(ConnectionGrantsLog.TenantNotFound(_logger, tenantId));
        }

        var connection = await _connections.Get(connectionName, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is not { } opened)
        {
            return GenericResult<DocumentPath>.Failure(ConnectionGrantsLog.ConnectionNotOpened(_logger, connectionName, connection.CurrentMessage));
        }

        return purposeOption.Unfit(opened) is { } reason
            ? GenericResult<DocumentPath>.Failure(ConnectionGrantsLog.UnfitForPurpose(_logger, connectionName, purpose, reason))
            : GenericResult<DocumentPath>.Success(parsed.Value);
    }

    // The live grant of a connection to a tenant for a purpose; Success with a null value when there is none.
    private async Task<IGenericResult<IConnectionGrantImplementationConfiguration?>> Find(Guid tenantId, string connectionName, string purpose, CancellationToken cancellationToken)
    {
        var all = await _grants.Get(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
        {
            return GenericResult<IConnectionGrantImplementationConfiguration?>.Failure(
                ConnectionGrantsLog.ReadFailed(_logger, $"The grants of tenant '{tenantId}'", all.CurrentMessage));
        }

        return GenericResult<IConnectionGrantImplementationConfiguration?>.Success(
            all.Value.FirstOrDefault(grant => grant.GranteeTenantId == tenantId && grant.IsCurrent && !grant.IsDeleted
                && string.Equals(grant.ConnectionName, connectionName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(grant.Purpose, purpose, StringComparison.OrdinalIgnoreCase)));
    }
}
