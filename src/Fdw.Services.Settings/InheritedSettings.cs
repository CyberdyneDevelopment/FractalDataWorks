using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>
/// Reads and writes the settings that inherit down the platform, tenant and organization tree and can be locked from above (FDW-1579).
/// </summary>
/// <remarks>
/// The platform's value is the <c>ServerSetting</c> row, a tenant's the <c>TenantSetting</c> row and an organization's the
/// <c>OrganizationSetting</c> row, so a value set through the older settings endpoints is the same value seen here. Walking down from the
/// platform, a level's value applies unless a level above it holds a lock; the first lock found governs every level below it.
/// </remarks>
public sealed class InheritedSettings : IInheritedSettings
{
    private readonly IServerSettingConfigurationProvider _server;
    private readonly ITenantSettingConfigurationProvider _tenants;
    private readonly IOrganizationSettingConfigurationProvider _organizations;
    private readonly IOrganizationScope _scope;
    private readonly ILogger<InheritedSettings> _logger;

    /// <summary>Initializes a new instance of the <see cref="InheritedSettings"/> class.</summary>
    /// <param name="server">Reads and writes the platform's settings.</param>
    /// <param name="tenants">Reads and writes the tenants' values.</param>
    /// <param name="organizations">Reads and writes the organizations' values.</param>
    /// <param name="scope">Finds an organization's tenant, and checks the caller's right at the organization's scope.</param>
    /// <param name="logger">The logger.</param>
    public InheritedSettings(
        IServerSettingConfigurationProvider server,
        ITenantSettingConfigurationProvider tenants,
        IOrganizationSettingConfigurationProvider organizations,
        IOrganizationScope scope,
        ILogger<InheritedSettings>? logger)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _logger = logger ?? NullLogger<InheritedSettings>.Instance;
    }

    // The rows that can matter to one scope, read once so every setting is judged against the same state.
    private sealed record Layers(
        Guid? TenantId,
        Guid? OrganizationId,
        IReadOnlyList<IServerSettingImplementationConfiguration> Server,
        IReadOnlyList<ITenantSettingImplementationConfiguration> Tenant,
        IReadOnlyList<IOrganizationSettingImplementationConfiguration> Organization);

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<EffectiveSetting>>> List(SettingScope scope, CancellationToken cancellationToken = default)
    {
        var loaded = await Load(scope, OrganizationRights.Read, cancellationToken).ConfigureAwait(false);
        return loaded.IsFailure || loaded.Value is not { } layers
            ? loaded.ToNewResult<IReadOnlyList<EffectiveSetting>>()
            : GenericResult<IReadOnlyList<EffectiveSetting>>.Success(
                [.. layers.Server.Where(row => row.IsActive).Select(row => Resolve(layers, row.Name)).OfType<EffectiveSetting>()]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EffectiveSetting>> Get(SettingScope scope, string settingName, CancellationToken cancellationToken = default)
    {
        var loaded = await Load(scope, OrganizationRights.Read, cancellationToken).ConfigureAwait(false);
        return loaded.IsFailure || loaded.Value is not { } layers
            ? loaded.ToNewResult<EffectiveSetting>()
            : ViewOf(layers, settingName);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EffectiveSetting>> Set(
        SettingScope scope, string settingName, string value, bool locked, CancellationToken cancellationToken = default)
    {
        var loaded = await Load(scope, OrganizationRights.Write, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure || loaded.Value is not { } layers)
        {
            return loaded.ToNewResult<EffectiveSetting>();
        }

        var refused = Guard(scope, layers, settingName, locked);
        if (refused is not null)
        {
            return GenericResult<EffectiveSetting>.Failure(refused);
        }

        var written = await Write(scope, layers, settingName, value, locked, cancellationToken).ConfigureAwait(false);
        if (written is not null)
        {
            return GenericResult<EffectiveSetting>.Failure(written);
        }

        InheritedSettingsLog.SettingSet(_logger, settingName, scope.Level, locked);
        return await Get(scope, settingName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EffectiveSetting>> Lift(SettingScope scope, string settingName, CancellationToken cancellationToken = default)
    {
        var loaded = await Load(scope, OrganizationRights.Write, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure || loaded.Value is not { } layers)
        {
            return loaded.ToNewResult<EffectiveSetting>();
        }

        var lifted = await Unlock(scope, layers, settingName, cancellationToken).ConfigureAwait(false);
        if (lifted is not null)
        {
            return GenericResult<EffectiveSetting>.Failure(lifted);
        }

        InheritedSettingsLog.LockLifted(_logger, settingName, scope.Level);
        return await Get(scope, settingName, cancellationToken).ConfigureAwait(false);
    }

    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    // Walks down from the platform: a level's value applies unless a level above holds a lock, and the first lock found is the one in force.
    private static EffectiveSetting? Resolve(Layers layers, string settingName)
    {
        var server = layers.Server.FirstOrDefault(row => Same(row.Name, settingName) && row.IsActive);
        if (server is null)
        {
            return null;
        }

        var value = server.SettingValue;
        var setAt = SettingScope.PlatformLevel;
        var lockedAt = server.IsLocked ? SettingScope.PlatformLevel : null;
        var tenant = lockedAt is null ? layers.Tenant.FirstOrDefault(row => Same(row.SettingName, settingName) && row.IsActive) : null;
        if (tenant is not null)
        {
            value = tenant.SettingValue;
            setAt = SettingScope.TenantLevel;
            lockedAt = tenant.IsLocked ? SettingScope.TenantLevel : null;
        }

        var organization = lockedAt is null ? layers.Organization.FirstOrDefault(row => Same(row.SettingName, settingName) && row.IsActive) : null;
        if (organization is not null)
        {
            value = organization.SettingValue;
            setAt = SettingScope.OrganizationLevel;
        }

        return new EffectiveSetting(server.Name, server.DataType, server.Description, value, setAt, lockedAt);
    }

    private IGenericResult<EffectiveSetting> ViewOf(Layers layers, string settingName)
    {
        var view = Resolve(layers, settingName);
        return view is null
            ? GenericResult<EffectiveSetting>.Failure(InheritedSettingsLog.SettingNotFound(_logger, settingName))
            : GenericResult<EffectiveSetting>.Success(view);
    }

    // The level above the scope that holds a lock on the setting, or null when none does.
    private static string? LockedAbove(SettingScope scope, Layers layers, string settingName)
    {
        if (string.Equals(scope.Level, SettingScope.PlatformLevel, StringComparison.Ordinal))
        {
            return null;
        }

        if (layers.Server.Any(row => Same(row.Name, settingName) && row.IsActive && row.IsLocked))
        {
            return SettingScope.PlatformLevel;
        }

        return string.Equals(scope.Level, SettingScope.OrganizationLevel, StringComparison.Ordinal)
            && layers.Tenant.Any(row => Same(row.SettingName, settingName) && row.IsActive && row.IsLocked)
                ? SettingScope.TenantLevel
                : null;
    }

    // The refusal for a write the rules do not allow, or null when it may go ahead.
    private IGenericMessage? Guard(SettingScope scope, Layers layers, string settingName, bool locked)
    {
        if (string.Equals(scope.Level, SettingScope.OrganizationLevel, StringComparison.Ordinal) && locked)
        {
            return InheritedSettingsLog.OrganizationCannotLock(_logger, settingName);
        }

        if (!string.Equals(scope.Level, SettingScope.PlatformLevel, StringComparison.Ordinal) && Resolve(layers, settingName) is null)
        {
            return InheritedSettingsLog.SettingNotFound(_logger, settingName);
        }

        return LockedAbove(scope, layers, settingName) is { } lockedAt
            ? InheritedSettingsLog.SettingLocked(_logger, settingName, scope.Level, lockedAt)
            : null;
    }

    private async Task<IGenericResult<Layers>> Load(SettingScope scope, string organizationRight, CancellationToken cancellationToken)
    {
        var tenantId = scope.TenantId;
        if (scope.OrganizationId is { } organizationId)
        {
            var found = await _scope.TenantOf(organizationRight, organizationId, cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return found.ToNewResult<Layers>();
            }

            tenantId = found.Value;
        }

        var server = await _server.Get(cancellationToken).ConfigureAwait(false);
        if (!server.IsSuccess || server.Value is null)
        {
            return GenericResult<Layers>.Failure(InheritedSettingsLog.ReadFailed(_logger, "The platform settings", server.CurrentMessage));
        }

        var tenants = await _tenants.Get(cancellationToken).ConfigureAwait(false);
        if (!tenants.IsSuccess || tenants.Value is null)
        {
            return GenericResult<Layers>.Failure(InheritedSettingsLog.ReadFailed(_logger, "The tenant settings", tenants.CurrentMessage));
        }

        var organizations = await _organizations.Get(cancellationToken).ConfigureAwait(false);
        if (!organizations.IsSuccess || organizations.Value is null)
        {
            return GenericResult<Layers>.Failure(InheritedSettingsLog.ReadFailed(_logger, "The organization settings", organizations.CurrentMessage));
        }

        return GenericResult<Layers>.Success(Narrow(tenantId, scope.OrganizationId, server.Value, tenants.Value, organizations.Value));
    }

    // Keeps only the tenant's and the organization's own rows; the platform's apply to everyone.
    private static Layers Narrow(
        Guid? tenantId,
        Guid? organizationId,
        IReadOnlyList<IServerSettingImplementationConfiguration> server,
        IReadOnlyList<ITenantSettingImplementationConfiguration> tenants,
        IReadOnlyList<IOrganizationSettingImplementationConfiguration> organizations)
        => new(
            tenantId,
            organizationId,
            server,
            [.. tenants.Where(row => tenantId is { } id && row.TenantId == id)],
            [.. organizations.Where(row => organizationId is { } id && row.OrganizationId == id)]);

    // Writes the value at the scope's level; the refusal if it could not, or null.
    private Task<IGenericMessage?> Write(SettingScope scope, Layers layers, string settingName, string value, bool locked, CancellationToken cancellationToken)
        => scope.Level switch
        {
            SettingScope.PlatformLevel => WritePlatform(layers, settingName, value, locked, cancellationToken),
            SettingScope.TenantLevel => WriteTenant(layers, settingName, value, locked, cancellationToken),
            _ => WriteOrganization(layers, settingName, value, cancellationToken),
        };

    private async Task<IGenericMessage?> WritePlatform(Layers layers, string settingName, string value, bool locked, CancellationToken cancellationToken)
    {
        var row = layers.Server.FirstOrDefault(existing => Same(existing.Name, settingName));
        if (row is null)
        {
            if (!SettingDefinitions.TryGet(settingName, out var definition))
            {
                return InheritedSettingsLog.SettingNotFound(_logger, settingName);
            }

            row = new ServerSettingImplementationConfiguration
            {
                Name = settingName,
                DataType = definition.DataType,
                Description = definition.Description,
                IsActive = true,
            };
        }

        row.SettingValue = value;
        row.IsLocked = locked;
        var saved = await _server.Save(row, "ServerSetting", "ServerSetting", row.Name, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? InheritedSettingsLog.WriteFailed(_logger, $"The platform value of '{settingName}'", saved.CurrentMessage) : null;
    }

    private async Task<IGenericMessage?> WriteTenant(Layers layers, string settingName, string value, bool locked, CancellationToken cancellationToken)
    {
        if (layers.TenantId is not { } tenantId)
        {
            return InheritedSettingsLog.ScopeIncomplete(_logger, SettingScope.TenantLevel, "tenant");
        }

        var row = layers.Tenant.FirstOrDefault(existing => Same(existing.SettingName, settingName));
        if (row is null)
        {
            row = new TenantSettingImplementationConfiguration
            {
                Name = $"{tenantId}/{settingName}",
                TenantId = tenantId,
                SettingName = settingName,
            };
        }

        row.SettingValue = value;
        row.IsActive = true;
        row.IsLocked = locked;
        var saved = await _tenants.Save(row, "TenantSetting", "TenantSetting", row.Name, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? InheritedSettingsLog.WriteFailed(_logger, $"The tenant value of '{settingName}'", saved.CurrentMessage) : null;
    }

    private async Task<IGenericMessage?> WriteOrganization(Layers layers, string settingName, string value, CancellationToken cancellationToken)
    {
        if (layers.TenantId is not { } tenantId || layers.OrganizationId is not { } organizationId)
        {
            return InheritedSettingsLog.ScopeIncomplete(_logger, SettingScope.OrganizationLevel, "organization");
        }

        var row = layers.Organization.FirstOrDefault(existing => Same(existing.SettingName, settingName));
        if (row is null)
        {
            row = new OrganizationSettingImplementationConfiguration
            {
                Name = $"{organizationId}/{settingName}",
                TenantId = tenantId,
                OrganizationId = organizationId,
                SettingName = settingName,
            };
        }

        row.SettingValue = value;
        row.IsActive = true;
        var saved = await _organizations.Save(row, "OrganizationSetting", "OrganizationSetting", row.Name, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? InheritedSettingsLog.WriteFailed(_logger, $"The organization value of '{settingName}'", saved.CurrentMessage) : null;
    }

    // Clears the lock a platform or tenant level holds; the refusal if there was none to clear, or null.
    private async Task<IGenericMessage?> Unlock(SettingScope scope, Layers layers, string settingName, CancellationToken cancellationToken)
    {
        if (string.Equals(scope.Level, SettingScope.PlatformLevel, StringComparison.Ordinal))
        {
            var server = layers.Server.FirstOrDefault(row => Same(row.Name, settingName) && row.IsLocked);
            if (server is null)
            {
                return InheritedSettingsLog.NotLocked(_logger, settingName, scope.Level);
            }

            server.IsLocked = false;
            var savedServer = await _server.Save(server, "ServerSetting", "ServerSetting", server.Name, cancellationToken).ConfigureAwait(false);
            return savedServer.IsFailure ? InheritedSettingsLog.WriteFailed(_logger, $"The platform value of '{settingName}'", savedServer.CurrentMessage) : null;
        }

        var tenant = string.Equals(scope.Level, SettingScope.TenantLevel, StringComparison.Ordinal)
            ? layers.Tenant.FirstOrDefault(row => Same(row.SettingName, settingName) && row.IsLocked)
            : null;
        if (tenant is null)
        {
            return InheritedSettingsLog.NotLocked(_logger, settingName, scope.Level);
        }

        tenant.IsLocked = false;
        var savedTenant = await _tenants.Save(tenant, "TenantSetting", "TenantSetting", tenant.Name, cancellationToken).ConfigureAwait(false);
        return savedTenant.IsFailure ? InheritedSettingsLog.WriteFailed(_logger, $"The tenant value of '{settingName}'", savedTenant.CurrentMessage) : null;
    }
}
