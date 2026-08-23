using Fdw.Configuration;
using Fdw.Services.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Domain configuration provider for health monitors — host-topology configuration bound from the
/// host's <c>HealthMonitors</c> appsettings section / environment variables (both flow through
/// <c>IConfiguration</c>), not from ConfigurationDb: which monitor implementation a host runs is
/// per-host, while ConfigurationDb rows are shared across every host.
/// </summary>
/// <remarks>
/// Implements the same <see cref="IServiceConfigurationProvider{TConfig}"/> seam every gateway-backed
/// domain uses, so <see cref="DefaultHealthMonitorProvider"/> resolves rows identically — only the
/// SOURCE differs. Reads fail loud on a missing row; writes fail loud (host configuration is not
/// mutable at runtime — silently accepting a Save would hide that nothing persisted). NO FALLBACKS.
/// </remarks>
public sealed class HealthMonitorConfigurationProvider : IServiceConfigurationProvider<HealthMonitorConfiguration>, IServiceConfigurationProvider
{
    private readonly IOptionsMonitor<List<HealthMonitorConfiguration>> _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitorConfigurationProvider"/> class.
    /// </summary>
    public HealthMonitorConfigurationProvider(
        IOptionsMonitor<List<HealthMonitorConfiguration>> options,
        ILogger<HealthMonitorConfigurationProvider>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<HealthMonitorConfigurationProvider>.Instance;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<HealthMonitorConfiguration>> Get(string name, CancellationToken ct = default)
    {
        var match = _options.CurrentValue.FirstOrDefault(
            c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? Task.FromResult(GenericResult<HealthMonitorConfiguration>.Failure(
                HealthMonitorLog.MonitorRowNotFound(_logger, name)))
            : Task.FromResult(GenericResult<HealthMonitorConfiguration>.Success(match));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<HealthMonitorConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        var match = _options.CurrentValue.FirstOrDefault(c => c.Id == id);
        return match is null
            ? Task.FromResult(GenericResult<HealthMonitorConfiguration>.Failure(
                HealthMonitorLog.MonitorRowNotFound(_logger, id.ToString())))
            : Task.FromResult(GenericResult<HealthMonitorConfiguration>.Success(match));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<HealthMonitorConfiguration>>> Get(CancellationToken ct = default)
        => Task.FromResult(GenericResult<IReadOnlyList<HealthMonitorConfiguration>>.Success(_options.CurrentValue));

    /// <inheritdoc/>
    public Task<IGenericResult<HealthMonitorConfiguration>> Save(HealthMonitorConfiguration record, CancellationToken ct = default)
        => Task.FromResult(GenericResult<HealthMonitorConfiguration>.Failure(
            HealthMonitorLog.WriteNotSupported(_logger, nameof(Save))));

    /// <inheritdoc/>
    public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
        => Task.FromResult(GenericResult.Failure(
            HealthMonitorLog.WriteNotSupported(_logger, nameof(Delete))));

    /// <inheritdoc/>
    public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
        => Task.FromResult(GenericResult.Failure(
            HealthMonitorLog.WriteNotSupported(_logger, nameof(Delete))));

    // ── Type-erased surface ─────────────────────────────────────────────────
    // Why explicit: only a parent provider calls these, and it holds this provider as the non-generic
    // IServiceConfigurationProvider. Delegating keeps one implementation of each operation.

    async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.Get(Guid id, CancellationToken ct)
    {
        var result = await Get(id, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? result.ToNewResult<IGenericConfiguration>(result.Value!)
            : result.ToNewResult<IGenericConfiguration>();
    }

    async Task<IGenericResult> IServiceConfigurationProvider.Save(IGenericConfiguration record, CancellationToken ct)
    {
        if (record is not HealthMonitorConfiguration typed)
        {
            return GenericResult.Failure(
                ServicesResultCodes.ByName("ServiceCastFailed"),
                ResultDetails.Create("ExpectedType", nameof(HealthMonitorConfiguration), "ActualType", record?.GetType().Name ?? "null"));
        }

        return await Save(typed, ct).ConfigureAwait(false);
    }

}
