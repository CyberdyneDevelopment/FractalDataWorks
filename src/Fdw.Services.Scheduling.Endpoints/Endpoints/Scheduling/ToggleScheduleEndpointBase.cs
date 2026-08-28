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
