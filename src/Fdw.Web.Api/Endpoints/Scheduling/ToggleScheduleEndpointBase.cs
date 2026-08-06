using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for toggling a schedule's enabled/disabled status.
/// </summary>
/// <typeparam name="TConfig">The concrete schedule configuration type.</typeparam>
public abstract class ToggleScheduleEndpointBase<TConfig> : Endpoint<ToggleScheduleRequest, ScheduleDetailDto>
    where TConfig : ScheduleConfiguration
{
    // Why: ScheduleConfigurationProvider replaces IOptionsMonitor<List<T>> with dual-source
    // (ctrl + cfg) provider that merges system and user configurations.
    private readonly ScheduleConfigurationProvider _provider;

    /// <inheritdoc />
    protected ToggleScheduleEndpointBase(ScheduleConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected virtual string ResourceName => "schedules";

    /// <summary>Gets the authorization policy name for write operations.</summary>
    protected virtual string WritePolicy => $"{ResourceName}:write";

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        // Why POST: every other sub-resource action in this API is a POST — /connections/{Name}/test,
        // /connections/{Name}/schema/refresh, /connections/test-config, /proxy/etl/trigger. This endpoint
        // was the only one declaring PUT, and ScheduleHttpClient has always POSTed to it, so toggling a
        // schedule returned 405 from the UI. The client follows the convention; the route did not.
        Post($"/{ResourceName}/{{Name}}/toggle");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Toggle {ResourceName} enabled status";
            s.Description = $"Enables or disables a {ResourceName} by name.";
        });
    }

    /// <summary>Toggles the schedule's enabled status after verifying existence.</summary>
    public override async Task HandleAsync(ToggleScheduleRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var existingResult = await _provider.Get(req.Name, ct).ConfigureAwait(false);

        if (!existingResult.IsSuccess || existingResult.Value == null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var updated = UpdateEnabledStatus((TConfig)existingResult.Value, req.IsEnabled);

        var saveResult = await _provider.Save(updated, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = "Failed to save schedule" }, ct).ConfigureAwait(false);
            return;
        }

        var detail = MapToDetail(updated);
        await Send.OkAsync(detail, ct).ConfigureAwait(false);
    }

    /// <summary>Updates the enabled status on the configuration. Override to add type-specific behavior.</summary>
    protected virtual TConfig UpdateEnabledStatus(TConfig config, bool isEnabled)
    {
        config.IsEnabled = isEnabled;
        return config;
    }

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapToDetail(TConfig config);
}
