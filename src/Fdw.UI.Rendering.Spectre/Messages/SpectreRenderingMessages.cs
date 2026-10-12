using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Rendering.Spectre.Messages;

/// <summary>
/// Log messages for Spectre.Console rendering operations.
/// </summary>
[MessageLoggingTypeCode("SPECTRE")]
public static partial class SpectreRenderingMessages
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

    /// <summary>
    /// Logs when prompting user for input.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Prompting user for '{componentId}' ({componentType})")]
    public static partial IGenericMessage PromptingUser(
        ILogger logger,
        string componentId,
        string componentType);

    /// <summary>
    /// Logs when user cancels a prompt.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "User cancelled prompt for '{componentId}'")]
    public static partial IGenericMessage UserCancelled(
        ILogger logger,
        string componentId);

    /// <summary>
    /// Logs when user saves configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "User saved configuration for page '{pageTitle}'")]
    public static partial IGenericMessage ConfigurationSaved(
        ILogger logger,
        string pageTitle);

    /// <summary>
    /// Logs when user requests deletion.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Warning,
        Message = "User requested deletion for page '{pageTitle}'")]
    public static partial IGenericMessage DeletionRequested(
        ILogger logger,
        string pageTitle);

    /// <summary>
    /// Logs when a component type is not supported.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "Unsupported component type '{componentType}' for component '{componentId}'")]
    public static partial IGenericMessage UnsupportedComponentType(
        ILogger logger,
        string componentType,
        string componentId);

    /// <summary>
    /// Logs when theme is applied.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Applied theme '{themeName}' with color palette '{paletteName}'")]
    public static partial IGenericMessage ThemeApplied(
        ILogger logger,
        string themeName,
        string paletteName);
}
