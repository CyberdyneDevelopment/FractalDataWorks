using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DeleteCommands = Fdw.Commands.Data.Extensions.Delete;
using UpdateCommands = Fdw.Commands.Data.Extensions.Update;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Reads and writes the retention policies of organizations, over the table named by <see cref="RetentionTables"/> (FDW-1575).
/// </summary>
/// <remarks>
/// An organization has at most one policy per document type; setting the policy of a type that has one changes it in place. Every method
/// first asks whether the caller holds the right for the organization (<see cref="RetentionScope"/>): reading needs <c>retention:read</c>,
/// changing needs <c>retention:write</c>.
/// </remarks>
public sealed class RetentionPolicies : IRetentionPolicies
{
    private readonly RetentionStore _store;
    private readonly RetentionScope _scope;
    private readonly ILogger<RetentionPolicies> _logger;

    /// <summary>Initializes a new instance of the <see cref="RetentionPolicies"/> class.</summary>
    /// <param name="gateways">Supplies the configuration gateway of the tables.</param>
    /// <param name="tables">Names the store, path and tables retention is kept in.</param>
    /// <param name="organizations">Reads an organization, which names its tenant.</param>
    /// <param name="authority">Says whether the caller holds a right for an organization.</param>
    /// <param name="logger">The logger.</param>
    public RetentionPolicies(
        IConfigurationGatewayProvider gateways, RetentionTables tables, IOrganizationProvider organizations, IScopeAuthority authority,
        ILogger<RetentionPolicies>? logger)
    {
        _logger = logger ?? NullLogger<RetentionPolicies>.Instance;
        _store = new RetentionStore(gateways, tables, _logger);
        _scope = new RetentionScope(organizations, authority, _logger);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<RetentionPolicy>>> List(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(RetentionScope.PolicyReadRight, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<IReadOnlyList<RetentionPolicy>>();
        }

        var rows = await Rows(tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        return rows.IsFailure || rows.Value is not { } found
            ? rows.ToNewResult<IReadOnlyList<RetentionPolicy>>()
            : GenericResult<IReadOnlyList<RetentionPolicy>>.Success([.. found.Select(Policy)]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<RetentionPolicy>> Set(
        Guid organizationId, NewRetentionPolicy policy, CancellationToken cancellationToken = default)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        var tenant = await _scope.TenantOf(RetentionScope.PolicyWriteRight, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<RetentionPolicy>();
        }

        if (Refusal(policy) is { } refusal)
        {
            return GenericResult<RetentionPolicy>.Failure(RetentionLog.PolicyInvalid(_logger, refusal));
        }

        var rows = await Rows(tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } found)
        {
            return rows.ToNewResult<RetentionPolicy>();
        }

        var existing = found.FirstOrDefault(row => string.Equals(row.DocumentType, policy.DocumentType, StringComparison.OrdinalIgnoreCase));
        var gateway = _store.Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult<RetentionPolicy>.Failure(RetentionLog.WriteFailed(_logger, What(policy.DocumentType, organizationId), gateway.CurrentMessage));
        }

        var id = existing is null ? Guid.CreateVersion7() : existing.Id;
        var written = await Save(resolved, tenant.Value, organizationId, id, existing is null, policy, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return GenericResult<RetentionPolicy>.Failure(RetentionLog.WriteFailed(_logger, What(policy.DocumentType, organizationId), written.CurrentMessage));
        }

        RetentionLog.PolicySet(_logger, policy.DocumentType, organizationId, policy.Years, policy.Months, policy.Days, policy.StartField, policy.EndAction);
        return GenericResult<RetentionPolicy>.Success(
            new RetentionPolicy(id, organizationId, policy.DocumentType, policy.Years, policy.Months, policy.Days, policy.StartField, policy.EndAction));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Remove(Guid organizationId, string documentType, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(RetentionScope.PolicyWriteRight, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant;
        }

        var rows = await Rows(tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } found)
        {
            return rows;
        }

        var existing = found.FirstOrDefault(row => string.Equals(row.DocumentType, documentType, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            return GenericResult.Failure(RetentionLog.PolicyNotFound(_logger, documentType, organizationId));
        }

        var gateway = _store.Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult.Failure(RetentionLog.WriteFailed(_logger, What(documentType, organizationId), gateway.CurrentMessage));
        }

        var removed = await resolved.Execute<int>(
            DeleteCommands.From(_store.Tables.PolicyTable).DataStore(_store.Tables.DataStoreName).Path(_store.Tables.PathName).Where("Id", existing.Id).Build(),
            cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return GenericResult.Failure(RetentionLog.WriteFailed(_logger, What(documentType, organizationId), removed.CurrentMessage));
        }

        RetentionLog.PolicyRemoved(_logger, existing.DocumentType, organizationId);
        return GenericResult.Success();
    }

    // Writes a new policy row, or the changed columns of the row a type already has.
    private Task<IGenericResult<int>> Save(
        IConfigurationGateway gateway, Guid tenantId, Guid organizationId, Guid id, bool isNew, NewRetentionPolicy policy, CancellationToken cancellationToken)
        => isNew
            ? gateway.Execute<int>(
                Insert.Into<RetentionPolicyRow>(_store.Tables.PolicyTable).DataStore(_store.Tables.DataStoreName).Path(_store.Tables.PathName)
                    .Value(new RetentionPolicyRow
                    {
                        Id = id, TenantId = tenantId, OrganizationId = organizationId, DocumentType = policy.DocumentType,
                        PeriodYears = policy.Years, PeriodMonths = policy.Months, PeriodDays = policy.Days, StartField = policy.StartField, EndAction = policy.EndAction,
                    }),
                cancellationToken)
            : gateway.Execute<int>(
                UpdateCommands.In<RetentionPolicyUpdate>(_store.Tables.PolicyTable).DataStore(_store.Tables.DataStoreName).Path(_store.Tables.PathName)
                    .Where("Id", id)
                    .Value(new RetentionPolicyUpdate
                    {
                        PeriodYears = policy.Years, PeriodMonths = policy.Months, PeriodDays = policy.Days, StartField = policy.StartField, EndAction = policy.EndAction,
                    }),
                cancellationToken);

    private static string What(string documentType, Guid organizationId) => $"The retention policy for '{documentType}' of organization '{organizationId}'";

    private static RetentionPolicy Policy(RetentionPolicyRow row)
        => new(row.Id, row.OrganizationId, row.DocumentType, row.PeriodYears, row.PeriodMonths, row.PeriodDays, row.StartField, row.EndAction);

    // Why a policy is refused, or null when it can be kept: a blank name, or a period that is negative or empty.
    private static string? Refusal(NewRetentionPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(policy.DocumentType) || string.IsNullOrWhiteSpace(policy.StartField) || string.IsNullOrWhiteSpace(policy.EndAction))
        {
            return "The document type, the field the period counts from and the end action are all required";
        }

        return PeriodRefusal(policy);
    }

    private static string? PeriodRefusal(NewRetentionPolicy policy)
    {
        if (policy.Years < 0 || policy.Months < 0 || policy.Days < 0)
        {
            return "A period cannot be negative";
        }

        return policy.Years + policy.Months + policy.Days == 0 ? "A period must be longer than zero" : null;
    }

    private Task<IGenericResult<IReadOnlyList<RetentionPolicyRow>>> Rows(Guid tenantId, Guid organizationId, CancellationToken cancellationToken)
        => _store.Rows<RetentionPolicyRow>(_store.Tables.PolicyTable, tenantId, organizationId, $"The retention policies of organization '{organizationId}'", cancellationToken);
}
