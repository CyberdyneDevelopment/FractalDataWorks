using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Wizard;

/// <summary>
/// MessageLogging for <see cref="WizardProviderBase{TContext}"/> operations.
/// EventId range: 4600-4620
/// </summary>
[MessageLoggingTypeCode("WIZARD")]
public static partial class WizardProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (4600-4602)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the wizard step changes.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="step">The step the wizard changed to.</param>
    /// <param name="stepCount">The total number of steps in the wizard.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Wizard step changed to {step} of {stepCount}")]
    public static partial IGenericMessage StepChanged(ILogger logger, int step, int stepCount);

    /// <summary>Logs when the wizard context is rebuilt.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="step">The step at which the context was rebuilt.</param>
    /// <param name="stepCount">The total number of steps in the wizard.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "Wizard context rebuilt at step {step} of {stepCount}")]
    public static partial IGenericMessage ContextRebuilt(ILogger logger, int step, int stepCount);

    /// <summary>Logs when initial data loading begins.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Wizard initial data loading started")]
    public static partial IGenericMessage InitialDataLoading(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug (4605-4606)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when initial data loading completes successfully.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Wizard initial data loaded successfully")]
    public static partial IGenericMessage InitialDataLoaded(ILogger logger);

    /// <summary>Logs when step validation begins.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="step">The step for which validation started.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Wizard validation started for step {step}")]
    public static partial IGenericMessage ValidationStarted(ILogger logger, int step);

    // ═══════════════════════════════════════════════════════════════════════════
    // Info (4610)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the wizard completes successfully.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Wizard completed successfully")]
    public static partial IGenericMessage WizardCompleted(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warn (4614-4615)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when validation fails and blocks step advancement.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="step">The step whose advancement was blocked by the validation failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Wizard validation failed — step {step} advancement blocked")]
    public static partial IGenericMessage ValidationFailed(ILogger logger, int step);

    /// <summary>Logs when a step is blocked by a validation gate.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="step">The step that was blocked by validation.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Warning,
        Message = "Wizard step {step} blocked by validation")]
    public static partial IGenericMessage StepBlockedByValidation(ILogger logger, int step);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (4618-4619)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when initial data loading fails with an exception.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused initial data loading to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Wizard initial data loading failed")]
    public static partial IGenericMessage LoadInitialDataFailed(ILogger logger, Exception exception);

    /// <summary>Logs when a Run operation fails with an exception.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the wizard operation to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Wizard operation failed")]
    public static partial IGenericMessage OperationFailed(ILogger logger, Exception exception);
}
