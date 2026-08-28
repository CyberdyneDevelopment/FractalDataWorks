using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Rendering.Blazor.Messages;

/// <summary>
/// Log messages for Blazor rendering operations.
/// </summary>
[MessageLoggingTypeCode("BLAZORUI")]
public static partial class BlazorRenderingMessages
{
    /// <summary>
    /// Logs when rendering a component.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Rendering component {componentId} of type {componentType}")]
    public static partial IGenericMessage RenderingComponent(
        ILogger logger,
        string componentId,
        string componentType);

    /// <summary>
    /// Logs when a page is rendered.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Rendered page '{pageTitle}' with {sectionCount} sections")]
    public static partial IGenericMessage PageRendered(
        ILogger logger,
        string pageTitle,
        int sectionCount);

    /// <summary>
    /// Logs when a prompt completes with a value.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Prompt completed for component '{componentId}'")]
    public static partial IGenericMessage PromptCompleted(
        ILogger logger,
        string componentId);

    /// <summary>
    /// Logs when a prompt is cancelled.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Prompt cancelled for component '{componentId}'")]
    public static partial IGenericMessage PromptCancelled(
        ILogger logger,
        string componentId);

    /// <summary>
    /// Logs when a page save is accepted.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Configuration saved from page '{pageTitle}'")]
    public static partial IGenericMessage ConfigurationSaved(
        ILogger logger,
        string pageTitle);

    /// <summary>
    /// Logs when a page delete is confirmed.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Deletion requested from page '{pageTitle}'")]
    public static partial IGenericMessage DeletionRequested(
        ILogger logger,
        string pageTitle);

    /// <summary>
    /// Logs when validation fails.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Validation failed for component '{componentId}': {validationMessage}")]
    public static partial IGenericMessage ValidationFailed(
        ILogger logger,
        string componentId,
        string validationMessage);

    /// <summary>
    /// Logs when a component model type has no Blazor mapping.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Warning,
        Message = "Unsupported component type '{componentType}' for component '{componentId}'")]
    public static partial IGenericMessage UnsupportedComponentType(
        ILogger logger,
        string componentType,
        string componentId);

    /// <summary>
    /// Logs when a render error occurs.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Render error for component '{componentId}': {errorMessage}")]
    public static partial IGenericMessage RenderError(
        ILogger logger,
        string componentId,
        string errorMessage);
}
