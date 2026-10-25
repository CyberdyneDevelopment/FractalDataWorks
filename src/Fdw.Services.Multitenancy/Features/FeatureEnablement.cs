using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Reads and writes the features switched on for tenants and organizations, over the tables named by <see cref="FeatureTables"/> (FDW-1580).
/// </summary>
/// <remarks>
/// A feature is on for a tenant while it has a row in the tenant table, and on for an organization while the tenant also has a row for it
/// in the organization table. Switching off deletes the row, because the tenant provider reads every row of the tenant table as an
/// enabled feature. Every call first asks whether the caller holds the right for what it names (<see cref="FeatureScope"/>): the organization
/// calls <c>organizations:read</c> or <c>organizations:write</c> for the organization, the two tenant calls <c>tenants:read</c> or
/// <c>tenants:write</c> for the whole company.
/// </remarks>
public sealed class FeatureEnablement : IFeatureEnablement
{
    private const string TenantLevel = "tenant";
    private const string OrganizationLevel = "organization";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly FeatureTables _tables;
    private readonly FeatureScope _scope;
    private readonly ILogger<FeatureEnablement> _logger;

    /// <summary>Initializes a new instance of the <see cref="FeatureEnablement"/> class.</summary>
    /// <param name="gateways">Supplies the configuration gateway of the tables.</param>
    /// <param name="tables">Names the store, path and tables the switches are kept in.</param>
    /// <param name="organizations">Reads an organization, which names its tenant.</param>
    /// <param name="authority">Says whether the caller holds a right for an organization.</param>
    /// <param name="logger">The logger.</param>
    public FeatureEnablement(
        IConfigurationGatewayProvider gateways, FeatureTables tables, IOrganizationProvider organizations, IScopeAuthority authority,
        ILogger<FeatureEnablement>? logger)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _tables = tables ?? throw new ArgumentNullException(nameof(tables));
        _logger = logger ?? NullLogger<FeatureEnablement>.Instance;
        _scope = new FeatureScope(organizations, authority, _logger);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<FeatureState>>> ForTenant(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var may = await _scope.MayActForTenant(FeatureScope.TenantReadRight, tenantId, cancellationToken).ConfigureAwait(false);
        return may.IsFailure
            ? may.ToNewResult<IReadOnlyList<FeatureState>>()
            : await TenantStates(tenantId, cancellationToken).ConfigureAwait(false);
    }

    // The features of a tenant, with the check already made.
    private async Task<IGenericResult<IReadOnlyList<FeatureState>>> TenantStates(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantRows = await TenantRows(tenantId, cancellationToken).ConfigureAwait(false);
        return tenantRows.IsFailure || tenantRows.Value is not { } rows
            ? tenantRows.ToNewResult<IReadOnlyList<FeatureState>>()
            : GenericResult<IReadOnlyList<FeatureState>>.Success([.. Features.All().Select(feature => TenantState(feature, rows))]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<FeatureState>>> ForOrganization(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(FeatureScope.ReadRight, organizationId, cancellationToken).ConfigureAwait(false);
        return tenant.IsFailure
            ? tenant.ToNewResult<IReadOnlyList<FeatureState>>()
            : await StatesOf(tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
    }

    // The features of an organization of a tenant, with the check already made.
    private async Task<IGenericResult<IReadOnlyList<FeatureState>>> StatesOf(Guid tenantId, Guid organizationId, CancellationToken cancellationToken)
    {
        var tenantRows = await TenantRows(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenantRows.IsFailure || tenantRows.Value is not { } tenant)
        {
            return tenantRows.ToNewResult<IReadOnlyList<FeatureState>>();
        }

        var organizationRows = await OrganizationRows(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        return organizationRows.IsFailure || organizationRows.Value is not { } organization
            ? organizationRows.ToNewResult<IReadOnlyList<FeatureState>>()
            : GenericResult<IReadOnlyList<FeatureState>>.Success([.. Features.All().Select(feature => OrganizationState(feature, tenant, organization))]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<FeatureState>> SetForTenant(Guid tenantId, string featureName, bool enabled, CancellationToken cancellationToken = default)
    {
        var may = await _scope.MayActForTenant(FeatureScope.TenantWriteRight, tenantId, cancellationToken).ConfigureAwait(false);
        if (may.IsFailure)
        {
            return may.ToNewResult<FeatureState>();
        }

        var feature = Features.ByName(featureName);
        if (feature.IsEmpty)
        {
            return GenericResult<FeatureState>.Failure(FeatureEnablementLog.FeatureNotFound(_logger, featureName));
        }

        var tenantRows = await TenantRows(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenantRows.IsFailure || tenantRows.Value is not { } rows)
        {
            return tenantRows.ToNewResult<FeatureState>();
        }

        var refused = await SwitchTenant(tenantId, feature.Name, enabled, rows, cancellationToken).ConfigureAwait(false);
        if (refused is not null)
        {
            return GenericResult<FeatureState>.Failure(refused);
        }

        var after = await TenantStates(tenantId, cancellationToken).ConfigureAwait(false);
        return after.IsFailure || after.Value is not { } states
            ? after.ToNewResult<FeatureState>()
            : GenericResult<FeatureState>.Success(states.Single(state => Same(state.Name, feature.Name)));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<FeatureState>> SetForOrganization(
        Guid organizationId, string featureName, bool enabled, CancellationToken cancellationToken = default)
    {
        var scoped = await _scope.TenantOf(FeatureScope.WriteRight, organizationId, cancellationToken).ConfigureAwait(false);
        if (scoped.IsFailure)
        {
            return scoped.ToNewResult<FeatureState>();
        }

        var tenantId = scoped.Value;
        var feature = Features.ByName(featureName);
        if (feature.IsEmpty)
        {
            return GenericResult<FeatureState>.Failure(FeatureEnablementLog.FeatureNotFound(_logger, featureName));
        }

        var tenantRows = await TenantRows(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenantRows.IsFailure || tenantRows.Value is not { } tenant)
        {
            return tenantRows.ToNewResult<FeatureState>();
        }

        if (enabled && !tenant.Any(row => Same(row.Feature, feature.Name)))
        {
            return GenericResult<FeatureState>.Failure(FeatureEnablementLog.NotEnabledForTenant(_logger, feature.Name, organizationId, tenantId));
        }

        var organizationRows = await OrganizationRows(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (organizationRows.IsFailure || organizationRows.Value is not { } organization)
        {
            return organizationRows.ToNewResult<FeatureState>();
        }

        var refused = await SwitchOrganization(tenantId, organizationId, feature.Name, enabled, organization, cancellationToken).ConfigureAwait(false);
        if (refused is not null)
        {
            return GenericResult<FeatureState>.Failure(refused);
        }

        var after = await StatesOf(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        return after.IsFailure || after.Value is not { } states
            ? after.ToNewResult<FeatureState>()
            : GenericResult<FeatureState>.Success(states.Single(state => Same(state.Name, feature.Name)));
    }

    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static FeatureState TenantState(IFeature feature, IReadOnlyList<TenantFeatureRow> tenant)
    {
        var on = tenant.Any(row => Same(row.Feature, feature.Name));
        return new FeatureState(feature.Name, feature.DisplayName, feature.Description, on, on ? null : FeatureState.PlatformLevel);
    }

    private static FeatureState OrganizationState(IFeature feature, IReadOnlyList<TenantFeatureRow> tenant, IReadOnlyList<OrganizationFeatureRow> organization)
    {
        if (!tenant.Any(row => Same(row.Feature, feature.Name)))
        {
            return new FeatureState(feature.Name, feature.DisplayName, feature.Description, false, FeatureState.PlatformLevel);
        }

        var on = organization.Any(row => Same(row.Feature, feature.Name));
        return new FeatureState(feature.Name, feature.DisplayName, feature.Description, on, on ? null : FeatureState.TenantLevel);
    }

    // The refusal for a switch that could not be written, or null; a switch to the state a feature is already in writes nothing.
    private async Task<IGenericMessage?> SwitchTenant(
        Guid tenantId, string feature, bool enabled, IReadOnlyList<TenantFeatureRow> rows, CancellationToken cancellationToken)
    {
        var present = rows.Any(row => Same(row.Feature, feature));
        if (enabled == present)
        {
            return null;
        }

        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return FeatureEnablementLog.WriteFailed(_logger, $"The feature '{feature}' of tenant '{tenantId}'", gateway.CurrentMessage);
        }

        var written = enabled
            ? await resolved.Execute<int>(
                Insert.Into<TenantFeatureRow>(_tables.TenantTable).DataStore(_tables.DataStoreName).Path(_tables.PathName)
                    .Value(new TenantFeatureRow { Id = Guid.CreateVersion7(), TenantId = tenantId, Feature = feature }),
                cancellationToken).ConfigureAwait(false)
            : await resolved.Execute<int>(
                Delete.From(_tables.TenantTable).DataStore(_tables.DataStoreName).Path(_tables.PathName)
                    .Where("TenantId", tenantId).Where("Feature", feature).Build(),
                cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return FeatureEnablementLog.WriteFailed(_logger, $"The feature '{feature}' of tenant '{tenantId}'", written.CurrentMessage);
        }

        LogSwitched(enabled, feature, TenantLevel, tenantId);
        return null;
    }

    private async Task<IGenericMessage?> SwitchOrganization(
        Guid tenantId, Guid organizationId, string feature, bool enabled, IReadOnlyList<OrganizationFeatureRow> rows, CancellationToken cancellationToken)
    {
        var present = rows.Any(row => Same(row.Feature, feature));
        if (enabled == present)
        {
            return null;
        }

        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return FeatureEnablementLog.WriteFailed(_logger, $"The feature '{feature}' of organization '{organizationId}'", gateway.CurrentMessage);
        }

        var written = enabled
            ? await resolved.Execute<int>(
                Insert.Into<OrganizationFeatureRow>(_tables.OrganizationTable).DataStore(_tables.DataStoreName).Path(_tables.PathName)
                    .Value(new OrganizationFeatureRow { Id = Guid.CreateVersion7(), TenantId = tenantId, OrganizationId = organizationId, Feature = feature }),
                cancellationToken).ConfigureAwait(false)
            : await resolved.Execute<int>(
                Delete.From(_tables.OrganizationTable).DataStore(_tables.DataStoreName).Path(_tables.PathName)
                    .Where("OrganizationId", organizationId).Where("Feature", feature).Build(),
                cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return FeatureEnablementLog.WriteFailed(_logger, $"The feature '{feature}' of organization '{organizationId}'", written.CurrentMessage);
        }

        LogSwitched(enabled, feature, OrganizationLevel, organizationId);
        return null;
    }

    private void LogSwitched(bool enabled, string feature, string level, Guid id)
    {
        if (enabled)
        {
            FeatureEnablementLog.Enabled(_logger, feature, level, id);
            return;
        }

        FeatureEnablementLog.Disabled(_logger, feature, level, id);
    }

    private IGenericResult<IConfigurationGateway> Gateway() => _gateways.Get(_tables.DataStoreName);

    private async Task<IGenericResult<IReadOnlyList<TenantFeatureRow>>> TenantRows(Guid tenantId, CancellationToken cancellationToken)
    {
        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult<IReadOnlyList<TenantFeatureRow>>.Failure(
                FeatureEnablementLog.ReadFailed(_logger, $"The features of tenant '{tenantId}'", gateway.CurrentMessage));
        }

        var read = await resolved.Execute<IEnumerable<TenantFeatureRow>>(
            Query.From<TenantFeatureRow>(_tables.DataStoreName, _tables.PathName, _tables.TenantTable).Where("TenantId", tenantId).Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } rows
            ? GenericResult<IReadOnlyList<TenantFeatureRow>>.Success([.. rows])
            : GenericResult<IReadOnlyList<TenantFeatureRow>>.Failure(
                FeatureEnablementLog.ReadFailed(_logger, $"The features of tenant '{tenantId}'", read.CurrentMessage));
    }

    private async Task<IGenericResult<IReadOnlyList<OrganizationFeatureRow>>> OrganizationRows(
        Guid tenantId, Guid organizationId, CancellationToken cancellationToken)
    {
        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult<IReadOnlyList<OrganizationFeatureRow>>.Failure(
                FeatureEnablementLog.ReadFailed(_logger, $"The features of organization '{organizationId}'", gateway.CurrentMessage));
        }

        var read = await resolved.Execute<IEnumerable<OrganizationFeatureRow>>(
            Query.From<OrganizationFeatureRow>(_tables.DataStoreName, _tables.PathName, _tables.OrganizationTable)
                .Where("TenantId", tenantId).Where("OrganizationId", organizationId).Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } rows
            ? GenericResult<IReadOnlyList<OrganizationFeatureRow>>.Success([.. rows])
            : GenericResult<IReadOnlyList<OrganizationFeatureRow>>.Failure(
                FeatureEnablementLog.ReadFailed(_logger, $"The features of organization '{organizationId}'", read.CurrentMessage));
    }
}
