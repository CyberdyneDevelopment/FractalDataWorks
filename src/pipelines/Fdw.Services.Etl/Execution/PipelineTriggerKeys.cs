using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Keeps the key of every scheduled fire the host has taken in etl.PipelineTriggerKey (FDW-4317). A key is claimed before the run is made and bound to the run once it exists, so a retry
/// of the fire finds the run and gets it back. How long a key is remembered is the setting <see cref="SettingDefinitions.PipelineTriggerKeyRetentionSeconds"/>; keys past it are removed as
/// the next fire claims one, so nothing else needs to sweep.
/// </summary>
public sealed class PipelineTriggerKeys : IPipelineTriggerKeys
{
    private const string PathName = "etl";
    private const string ContainerName = "PipelineTriggerKey";

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineTriggerKeys"/> class.</summary>
    /// <param name="scopes">Makes the scope each call works in, since the data gateways are scoped.</param>
    /// <param name="time">The clock the retention is measured by.</param>
    /// <param name="logger">The logger.</param>
    public PipelineTriggerKeys(IServiceScopeFactory scopes, TimeProvider time, ILogger<PipelineTriggerKeys>? logger = null)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? NullLogger<PipelineTriggerKeys>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineTriggerKeyClaim>> Claim(Guid tenantId, string key, DateTimeOffset fireTime, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, tenantId);
            var open = Open(scope.ServiceProvider);
            if (open.IsFailure)
            {
                return open.ToNewResult<PipelineTriggerKeyClaim>();
            }

            var retention = scope.ServiceProvider.GetRequiredService<IEffectiveSettingsProvider>().GetEffectiveValue<int>(SettingDefinitions.PipelineTriggerKeyRetentionSeconds);
            if (retention <= 0)
            {
                return GenericResult<PipelineTriggerKeyClaim>.Failure(PipelineTriggerKeyLog.RetentionNotSet(_logger, SettingDefinitions.PipelineTriggerKeyRetentionSeconds));
            }

            var oldest = _time.GetUtcNow().AddSeconds(-retention);
            if (fireTime < oldest)
            {
                return GenericResult<PipelineTriggerKeyClaim>.Failure(
                    PipelineTriggerKeyLog.KeyExpired(_logger, key, fireTime, retention, SettingDefinitions.PipelineTriggerKeyRetentionSeconds));
            }

            var (gateway, store) = open.Value;

            // Keys past the retention are removed as the next fire claims one: the fire time is what ages a key.
            var swept = await gateway.Execute<int>(
                Delete.From(ContainerName).DataStore(store).Path(PathName).Where("FireTime", FilterOperators.ByName("LessThan"), oldest).Build(), cancellationToken).ConfigureAwait(false);
            if (swept.IsFailure)
            {
                return GenericResult<PipelineTriggerKeyClaim>.Failure(PipelineTriggerKeyLog.KeyNotKept(_logger, key, swept.CurrentMessage));
            }

            var inserted = await gateway.Execute<int>(
                Insert.Into<EtlPipelineTriggerKeyRecord>(ContainerName).DataStore(store).Path(PathName)
                    .Value(new EtlPipelineTriggerKeyRecord { TenantId = tenantId, KeyText = key, FireTime = fireTime }),
                cancellationToken).ConfigureAwait(false);
            if (inserted.IsSuccess)
            {
                return GenericResult<PipelineTriggerKeyClaim>.Success(PipelineTriggerKeyClaim.Claimed);
            }

            // The insert was refused: most likely the unique key (tenant, key) is taken by an earlier call. Read it; a key that is not there means the insert failed for another reason.
            var held = await gateway.Execute<System.Collections.Generic.IEnumerable<EtlPipelineTriggerKeyRecord>>(
                Query.From<EtlPipelineTriggerKeyRecord>(store, PathName, ContainerName).Where("TenantId", tenantId).Where("KeyText", key).Build(), cancellationToken).ConfigureAwait(false);
            if (held.IsFailure || held.Value is null)
            {
                return GenericResult<PipelineTriggerKeyClaim>.Failure(PipelineTriggerKeyLog.KeyNotKept(_logger, key, held.CurrentMessage));
            }

            var earlier = held.Value.FirstOrDefault();
            if (earlier is null)
            {
                return GenericResult<PipelineTriggerKeyClaim>.Failure(PipelineTriggerKeyLog.KeyNotKept(_logger, key, inserted.CurrentMessage));
            }

            return earlier.ExecutionId is { } executionId
                ? GenericResult<PipelineTriggerKeyClaim>.Success(PipelineTriggerKeyClaim.Existing(executionId))
                : GenericResult<PipelineTriggerKeyClaim>.Failure(PipelineTriggerKeyLog.KeyInProgress(_logger, key));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Bind(Guid tenantId, string key, Guid executionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, tenantId);
            var open = Open(scope.ServiceProvider);
            if (open.IsFailure)
            {
                return open;
            }

            var bound = await open.Value.Gateway.Execute<int>(
                Update.In<EtlPipelineTriggerKeyBind>(ContainerName).DataStore(open.Value.Store).Path(PathName)
                    .Where("TenantId", tenantId).Where("KeyText", key)
                    .Value(new EtlPipelineTriggerKeyBind { ExecutionId = executionId }),
                cancellationToken).ConfigureAwait(false);
            return bound.IsSuccess && bound.Value > 0
                ? GenericResult.Success()
                : GenericResult.Failure(PipelineTriggerKeyLog.KeyNotBound(_logger, key, executionId, bound.IsSuccess ? "the key has no row" : bound.CurrentMessage));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Release(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, tenantId);
            var open = Open(scope.ServiceProvider);
            if (open.IsFailure)
            {
                return open;
            }

            var released = await open.Value.Gateway.Execute<int>(
                Delete.From(ContainerName).DataStore(open.Value.Store).Path(PathName).Where("TenantId", tenantId).Where("KeyText", key).Build(), cancellationToken).ConfigureAwait(false);
            return released.IsSuccess
                ? GenericResult.Success()
                : GenericResult.Failure(PipelineTriggerKeyLog.KeyNotBound(_logger, key, Guid.Empty, released.CurrentMessage));
        }
    }

    private IGenericResult<(IDataGateway Gateway, string Store)> Open(IServiceProvider services)
    {
        var store = EtlPipelineTypes.OperationalConnection;
        return string.IsNullOrWhiteSpace(store)
            ? GenericResult<(IDataGateway Gateway, string Store)>.Failure(PipelineTriggerKeyLog.StoreNotNamed(_logger))
            : GenericResult<(IDataGateway Gateway, string Store)>.Success((services.GetRequiredService<IDataGatewayProvider>().ByName(DataGatewayNames.Main), store));
    }

    // The bookkeeping context of the key: its tenant rides on it so the row lands under the fire's tenant.
    private static void Elevate(IServiceProvider services, Guid tenantId)
    {
        // Why required (FDW-4395): every host that runs the pipeline types registers the accessor, so an absent one is a host that is built wrong, and
        // the bookkeeping row would be written under no tenant context. It is a missing registration to be seen, not a case to step over.
        var accessor = services.GetRequiredService<IAuthenticationContextAccessor>();
        if (accessor.Current is not null)
        {
            return;
        }

        accessor.Current = new SystemAuthenticationContext(tenantId);
    }
}
