using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.LanguageModels.Logging;

/// <summary>
/// MessageLogging for the language model domain. Every failure a model or a provider returns is one of these coded messages, so a consumer
/// reads the code, never the text. The numbers follow the category bands of the result code catalog: where a canonical number exists
/// (rate limited, not configured, connection failed, required value missing, not found, not supported) it is that number.
/// </summary>
/// <remarks>
/// No message here ever carries a key or a token. A provider that reports a reason passes a short text of its own, never the request or the secret.
/// </remarks>
[MessageLoggingTypeCode("LANGUAGEMODELS")]
public static partial class LanguageModelLog
{
    /// <summary>Logs that a request passed the capability and effort check.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Request to language model '{modelName}' passed the capability and effort check")]
    public static partial IGenericMessage RequestChecked(ILogger logger, string modelName);

    /// <summary>Logs that a model replied.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="finishReason">The name of the finish reason.</param>
    /// <param name="inputTokens">The tokens the model read.</param>
    /// <param name="outputTokens">The tokens the model wrote.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Language model '{modelName}' replied: finish reason '{finishReason}', {inputTokens} input tokens, {outputTokens} output tokens")]
    public static partial IGenericMessage ReplyCompleted(ILogger logger, string modelName, string finishReason, int inputTokens, int outputTokens);

    /// <summary>Logs that a streamed reply ended.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="endKind">The name of the kind of event that ended the stream.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Streamed reply of language model '{modelName}' ended with a '{endKind}' event")]
    public static partial IGenericMessage StreamEnded(ILogger logger, string modelName, string endKind);

    /// <summary>Logs that a configured model has no model id, so no call is made.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "Language model '{modelName}' has no model id on its configuration row; a model id is required and none is applied")]
    public static partial IGenericMessage ModelIdMissing(ILogger logger, string modelName);

    /// <summary>Logs that a model cannot be used because it is not configured.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model that is not configured.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error,
        Message = "Language model '{modelName}' is not configured")]
    public static partial IGenericMessage ModelNotConfigured(ILogger logger, string modelName);

    /// <summary>Logs that a request is larger than the model's context.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "The request to language model '{modelName}' is larger than the model's context")]
    public static partial IGenericMessage ContextTooLarge(ILogger logger, string modelName);

    /// <summary>Logs that the provider filtered the content of a request or a reply.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning,
        Message = "The provider of language model '{modelName}' filtered the content of the request or the reply")]
    public static partial IGenericMessage ContentFiltered(ILogger logger, string modelName);

    /// <summary>Logs that the provider refused a request.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="reason">A short reason the provider gave, never carrying the request or a secret.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error,
        Message = "The provider of language model '{modelName}' refused the request: {reason}")]
    public static partial IGenericMessage ProviderRefused(ILogger logger, string modelName, string reason);

    /// <summary>Logs that a request needs a capability the model does not carry.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="capabilityName">The name of the capability the request needs.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error,
        Message = "The request to language model '{modelName}' needs the '{capabilityName}' capability, which the model does not carry; no call was made")]
    public static partial IGenericMessage CapabilityNotCarried(ILogger logger, string modelName, string capabilityName);

    /// <summary>Logs that a request states a reasoning effort the model does not carry.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="effortName">The name of the reasoning effort the request states.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error,
        Message = "The request to language model '{modelName}' states the '{effortName}' reasoning effort, which the model does not carry; no call was made")]
    public static partial IGenericMessage EffortNotCarried(ILogger logger, string modelName, string effortName);

    /// <summary>Logs that a request states no usable limit on the tokens the model may write.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error,
        Message = "The request to language model '{modelName}' states a limit of fewer than one output token; the caller states the limit and none is applied")]
    public static partial IGenericMessage MaxOutputTokensInvalid(ILogger logger, string modelName);

    /// <summary>Logs that a secret a model needs was not found in its secret manager.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="secretManagerName">The name of the secret manager asked.</param>
    /// <param name="secretName">The name the secret is asked for by. A name, never the value.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error,
        Message = "Language model '{modelName}': secret '{secretName}' was not found in secret manager '{secretManagerName}'")]
    public static partial IGenericMessage SecretNotFound(ILogger logger, string modelName, string secretManagerName, string secretName);

    /// <summary>Logs that the provider could not be reached or is down.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="reason">A short reason, such as a status code or an exception type, never carrying the request or a secret.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error,
        Message = "The provider of language model '{modelName}' is unavailable: {reason}")]
    public static partial IGenericMessage ProviderUnavailable(ILogger logger, string modelName, string reason);

    /// <summary>Logs that the provider limited the rate of calls.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 80001, Level = LogLevel.Warning,
        Message = "The provider of language model '{modelName}' limited the rate of calls; retry later")]
    public static partial IGenericMessage RateLimited(ILogger logger, string modelName);

    /// <summary>Logs that the provider stopped for a reason this contract does not carry.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <param name="providerReason">The reason the provider gave.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 90004, Level = LogLevel.Error,
        Message = "The provider of language model '{modelName}' stopped for the reason '{providerReason}', which has no finish reason in this contract")]
    public static partial IGenericMessage ProviderStopNotMapped(ILogger logger, string modelName, string providerReason);

    /// <summary>Logs that a generic command was sent to a language model.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="modelName">The name of the model configuration.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Language model '{modelName}' rejected a generic command; a language model takes a request through Reply or Stream only")]
    public static partial IGenericMessage GenericCommandRejected(ILogger logger, string modelName);
}
