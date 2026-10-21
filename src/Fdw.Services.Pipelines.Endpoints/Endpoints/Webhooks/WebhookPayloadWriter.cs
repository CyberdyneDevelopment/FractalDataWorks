using System;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Webhooks;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Turns a stored webhook trigger into the form the API answers in. The hash of the secret is never part of it.</summary>
public static class WebhookPayloadWriter
{
    /// <summary>Writes the stored trigger.</summary>
    /// <param name="trigger">The stored trigger.</param>
    /// <returns>The payload.</returns>
    public static WebhookPayload Write(WebhookTriggerConfiguration trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        return new WebhookPayload
        {
            Name = trigger.Name,
            Pipeline = trigger.PipelineName,
            Enabled = trigger.IsEnabled,
            Description = trigger.Description,
            RunRoles = [.. trigger.RunRoleNames],
            Parameters = [.. trigger.ParameterMaps.OrderBy(map => map.Ordinal).Select(map => new WebhookParameterMapPayload
            {
                BodyPath = map.BodyPath, Parameter = map.ParameterName,
            })],
            SecretPrefix = trigger.SecretPrefix,
            SecretCreatedAt = trigger.SecretCreatedAt,
        };
    }

    /// <summary>Writes the stored trigger as one row of a list.</summary>
    /// <param name="trigger">The stored trigger.</param>
    /// <returns>The row.</returns>
    public static WebhookSummaryPayload Summarize(WebhookTriggerConfiguration trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        return new WebhookSummaryPayload
        {
            Name = trigger.Name,
            Pipeline = trigger.PipelineName,
            Enabled = trigger.IsEnabled,
            Description = trigger.Description,
            SecretPrefix = trigger.SecretPrefix,
        };
    }
}
