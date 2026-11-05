using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Publishes the components of the runs in flight (FDW-3778). Every interval the setting
/// <see cref="SettingDefinitions.PipelineComponentsPublishSeconds"/> names, it reads each running run's components and links
/// from the live view FDW-3382 keeps, and broadcasts a run's list only when it differs from the last one sent for that run. It
/// adds no tracker of its own: the poll of GET /etl/queue and this push read the same record.
/// </summary>
public sealed class PipelineComponentsPublisher : BackgroundService
{
    private readonly PipelineRunScheduler _scheduler;
    private readonly RunningFlows _running;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineComponentsPublisher"/> class.</summary>
    public PipelineComponentsPublisher(
        PipelineRunScheduler scheduler,
        RunningFlows running,
        IEffectiveSettingsProvider settings,
        IServiceScopeFactory scopes,
        ILogger<PipelineComponentsPublisher>? logger = null)
    {
        _scheduler = scheduler;
        _running = running;
        _settings = settings;
        _scopes = scopes;
        _logger = logger ?? NullLogger<PipelineComponentsPublisher>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.TryGetEffectiveValue<int>(SettingDefinitions.PipelineComponentsPublishSeconds, out var seconds) || seconds <= 0)
        {
            PipelineComponentsPublisherLog.IntervalNotSet(_logger, SettingDefinitions.PipelineComponentsPublishSeconds);
            return;
        }

        var sent = new Dictionary<Guid, string>();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await PublishOnce(sent).ConfigureAwait(false);
        }
    }

    /// <summary>Reads the runs in flight once and broadcasts the lists that changed.</summary>
    /// <param name="sent">The fingerprint last sent for each run; a run that is no longer in flight is dropped from it.</param>
    internal async Task PublishOnce(Dictionary<Guid, string> sent)
    {
        var inFlight = _scheduler.Snapshot().Running.Where(entry => entry.Waiting is null).ToList();
        foreach (var gone in sent.Keys.Except(inFlight.Select(entry => entry.Run.ExecutionId)).ToList())
        {
            _ = sent.Remove(gone);
        }

        using var scope = _scopes.CreateScope();
        var broadcaster = scope.ServiceProvider.GetRequiredService<IPipelineStatusBroadcaster>();
        foreach (var entry in inFlight)
        {
            await PublishRun(broadcaster, entry, sent).ConfigureAwait(false);
        }
    }

    private async Task PublishRun(IPipelineStatusBroadcaster broadcaster, PipelineRunQueueEntry entry, Dictionary<Guid, string> sent)
    {
        var executionId = entry.Run.ExecutionId;
        if (_running.Components(executionId) is not { } components || _running.Links(executionId) is not { } links)
        {
            return;
        }

        var update = new PipelineComponentsUpdate
        {
            PipelineName = entry.Run.PipelineName,
            ExecutionId = executionId,
            Components = FlowLiveResponses.Components(components),
            Links = FlowLiveResponses.Links(links),
        };
        var fingerprint = JsonSerializer.Serialize(new { update.Components, update.Links });
        if (sent.TryGetValue(executionId, out var last) && string.Equals(last, fingerprint, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await broadcaster.BroadcastComponents(update, entry.Run.TenantId).ConfigureAwait(false);
            sent[executionId] = fingerprint;
        }
        catch (Exception exception)
        {
            PipelineComponentsPublisherLog.PublishFailed(_logger, exception, executionId);
        }
    }
}
