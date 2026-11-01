using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints.Logging;

/// <summary>
/// MessageLogging for the webhook trigger endpoints (FDW-1884): reading a stored trigger, and the calls to a trigger's address.
/// EventId range: 20994-20999.
/// </summary>
[MessageLoggingTypeCode("WEBHOOK")]
public static partial class WebhookEndpointLog
{
    /// <summary>Logs a webhook trigger that was saved and cannot be read back.</summary>
    [MessageLogging(EventId = 20994, Level = LogLevel.Error, Message = "Webhook trigger '{webhook}' was saved and cannot be read back")]
    public static partial IGenericMessage SavedButUnreadable(ILogger logger, string webhook);

    /// <summary>Logs a stored webhook trigger of an implementation this endpoint does not read.</summary>
    [MessageLogging(EventId = 20995, Level = LogLevel.Error, Message = "Webhook trigger '{webhook}' is a {implementation} trigger, not a Webhook trigger")]
    public static partial IGenericMessage NotAWebhookTrigger(ILogger logger, string webhook, string implementation);

    /// <summary>Logs a webhook trigger being made, without its secret.</summary>
    [MessageLogging(EventId = 20996, Level = LogLevel.Information, Message = "Made webhook trigger '{webhook}' for pipeline '{pipeline}' (secret {prefix}...)")]
    public static partial IGenericMessage Made(ILogger logger, string webhook, string pipeline, string prefix);

    /// <summary>Logs a new secret being made for a webhook trigger, without the secret.</summary>
    [MessageLogging(EventId = 20997, Level = LogLevel.Information, Message = "Made a new secret for webhook trigger '{webhook}' (now {prefix}...); the old one no longer works")]
    public static partial IGenericMessage NewSecretMade(ILogger logger, string webhook, string prefix);
}
