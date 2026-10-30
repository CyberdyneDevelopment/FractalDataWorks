using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint setting the settings of an existing notification of any type: <c>PUT /notifications/{Name}/settings</c> (FDW-2931).
/// </summary>
/// <remarks>
/// 204 when saved; 404 when there is no such notification; 400 naming a setting the type does not have, a value that does not read as the
/// setting's type, or a secret given as a value.
/// </remarks>
public abstract class SetNotificationSettingsEndpointBase : Endpoint<SetNotificationSettingsRequest>
{
    private readonly INotificationConfigurationProvider _provider;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SetNotificationSettingsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger for the endpoint.</param>
    /// <param name="provider">The notification domain's configuration provider.</param>
    protected SetNotificationSettingsEndpointBase(ILogger logger, INotificationConfigurationProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/notifications/{Name}/settings");
        Policies("notifications:write");
        Summary(s =>
        {
            s.Summary = "Set the settings of a notification";
            s.Description = "Sets the settings the notification's type declares. A secret setting is named with secretManagerName and secretKeyName on the notification, never given as a value.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add endpoint-specific configuration such as tags.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SetNotificationSettingsRequest req, CancellationToken ct)
    {
        NotificationEndpointLog.SettingNotificationSettings(_logger, req.Name);

        var existing = await _provider.Get(req.Name, ct).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            await Send.ResultAsync(Answer(existing)).ConfigureAwait(false);
            return;
        }

        if (existing.Value is not { } configuration)
        {
            await Send.ResultAsync(Answer(GenericResult.Failure(NotificationEndpointLog.NotificationNotFound(_logger, req.Name)))).ConfigureAwait(false);
            return;
        }

        var saved = await Save(req, configuration, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            await Send.ResultAsync(Answer(saved)).ConfigureAwait(false);
            return;
        }

        NotificationEndpointLog.NotificationSettingsSet(_logger, req.Name);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    // Sets the given settings on the configuration the type of the notification declares them for, then saves it.
    private async Task<IGenericResult> Save(SetNotificationSettingsRequest req, INotificationImplementationConfiguration configuration, CancellationToken ct)
    {
        var type = NotificationServiceTypes.All().Values.FirstOrDefault(
            candidate => string.Equals(candidate.Name, configuration.Implementation, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return GenericResult.Failure(NotificationEndpointLog.UnknownNotificationImplementation(
                _logger, req.Name, configuration.Implementation, string.Join(", ", NotificationServiceTypes.All().Values.Select(known => known.Name))));
        }

        var applied = NotificationSettings.Apply(type, configuration, req.Settings, creating: false, _logger);
        return applied.IsFailure
            ? applied
            : await _provider.Update(configuration, configuration.Domain, configuration.Implementation, configuration.Name, ct).ConfigureAwait(false);
    }

    private Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<Microsoft.AspNetCore.Mvc.ProblemDetails> Answer(IGenericResult failure)
    {
        var (status, problem) = ResultHttpStatusMapper.Map(failure, HttpContext);
        return Microsoft.AspNetCore.Http.TypedResults.Json(problem, statusCode: status);
    }
}
