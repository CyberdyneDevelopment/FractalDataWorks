using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings.Commands;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Settings;

/// <summary>
/// Composite configuration provider for the three-layer settings hierarchy: server → tenant → role.
/// Wraps three two-arity <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> instances.
/// </summary>
public class SettingsConfigurationProvider
{
    private readonly ImplementationConfigurationProviderBase<ServerSettingConfiguration, ServerSettingConfigurationCommand> _serverProvider;
    private readonly ImplementationConfigurationProviderBase<TenantSettingConfiguration, TenantSettingConfigurationCommand> _tenantProvider;
    private readonly ImplementationConfigurationProviderBase<RoleSettingConfiguration, RoleSettingConfigurationCommand> _roleProvider;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SettingsConfigurationProvider"/> class.</summary>
    public SettingsConfigurationProvider(
        ImplementationConfigurationProviderBase<ServerSettingConfiguration, ServerSettingConfigurationCommand> serverProvider,
        ImplementationConfigurationProviderBase<TenantSettingConfiguration, TenantSettingConfigurationCommand> tenantProvider,
        ImplementationConfigurationProviderBase<RoleSettingConfiguration, RoleSettingConfigurationCommand> roleProvider,
        ILogger<SettingsConfigurationProvider>? logger)
    {
        _serverProvider = serverProvider ?? throw new ArgumentNullException(nameof(serverProvider));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));
        _logger = logger ?? NullLogger<SettingsConfigurationProvider>.Instance;
    }

    /// <summary>Gets all server-level settings.</summary>
    public virtual async Task<IGenericResult<IReadOnlyList<ServerSettingConfiguration>>> GetServerSettings(CancellationToken cancellationToken = default)
    {
        var result = await _serverProvider.Get(cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
            SettingsConfigurationProviderLog.ServerSettingsLoaded(_logger, result.Value?.Count ?? 0);
        return result;
    }

    /// <summary>Gets a server setting by name.</summary>
    public virtual Task<IGenericResult<ServerSettingConfiguration>> GetServerSetting(string name, CancellationToken cancellationToken = default)
        => _serverProvider.Get(name, cancellationToken);

    /// <summary>Gets all tenant-level setting overrides.</summary>
    public virtual async Task<IGenericResult<IReadOnlyList<TenantSettingConfiguration>>> GetTenantSettings(CancellationToken cancellationToken = default)
    {
        var result = await _tenantProvider.Get(cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
            SettingsConfigurationProviderLog.TenantSettingsLoaded(_logger, result.Value?.Count ?? 0);
        return result;
    }

    /// <summary>Gets a tenant setting by name.</summary>
    public virtual Task<IGenericResult<TenantSettingConfiguration>> GetTenantSetting(string name, CancellationToken cancellationToken = default)
        => _tenantProvider.Get(name, cancellationToken);

    /// <summary>Gets all role-level setting overrides.</summary>
    public virtual async Task<IGenericResult<IReadOnlyList<RoleSettingConfiguration>>> GetRoleSettings(CancellationToken cancellationToken = default)
    {
        var result = await _roleProvider.Get(cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
            SettingsConfigurationProviderLog.RoleSettingsLoaded(_logger, result.Value?.Count ?? 0);
        return result;
    }

    /// <summary>Gets a role setting by name.</summary>
    public virtual Task<IGenericResult<RoleSettingConfiguration>> GetRoleSetting(string name, CancellationToken cancellationToken = default)
        => _roleProvider.Get(name, cancellationToken);

    /// <summary>Persists a server-level setting.</summary>
    public virtual Task<IGenericResult<ServerSettingConfiguration>> SaveServerSetting(ServerSettingConfiguration record, CancellationToken ct = default)
        => _serverProvider.Save(record, ct);

    /// <summary>Persists a tenant-level setting.</summary>
    public virtual Task<IGenericResult<TenantSettingConfiguration>> SaveTenantSetting(TenantSettingConfiguration record, CancellationToken ct = default)
        => _tenantProvider.Save(record, ct);

    /// <summary>Persists a role-level setting.</summary>
    public virtual Task<IGenericResult<RoleSettingConfiguration>> SaveRoleSetting(RoleSettingConfiguration record, CancellationToken ct = default)
        => _roleProvider.Save(record, ct);

    /// <summary>Deletes a server-level setting by Id.</summary>
    public virtual Task<IGenericResult> DeleteServerSetting(Guid id, CancellationToken ct = default)
        => _serverProvider.Delete(id, ct);

    /// <summary>Deletes a tenant-level setting by Id.</summary>
    public virtual Task<IGenericResult> DeleteTenantSetting(Guid id, CancellationToken ct = default)
        => _tenantProvider.Delete(id, ct);

    /// <summary>Deletes a role-level setting by Id.</summary>
    public virtual Task<IGenericResult> DeleteRoleSetting(Guid id, CancellationToken ct = default)
        => _roleProvider.Delete(id, ct);
}
