using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for step registration and flow validation.
/// </summary>
/// <remarks>EventId range: 91150–91157.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class StepResolverLog
{
    /// <summary>A step was registered under its name.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The name a flow will use.</param>
    /// <param name="implementation">The type registered.</param>
    [MessageLogging(EventId = 91150, Level = LogLevel.Trace,
        Message = "Step '{stepName}' registered as {implementation}")]
    internal static partial IGenericMessage Registered(
        ILogger<AuthenticationStepResolver> logger, string stepName, string implementation);

    /// <summary>A flow's steps all exist and are ordered so each has what it needs.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepCount">How many steps it has.</param>
    // Why Information: this runs once at startup per flow, and its absence from the log is how an
    // operator notices a flow that never loaded.
    [MessageLogging(EventId = 91151, Level = LogLevel.Information,
        Message = "Flow '{flowName}' validated: {stepCount} step(s), every requirement met in order")]
    internal static partial IGenericMessage FlowValidated(
        ILogger<AuthenticationStepResolver> logger, string flowName, int stepCount);

    /// <summary>No step name was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91152, Level = LogLevel.Error,
        Message = "A step name must be supplied")]
    internal static partial IGenericMessage NameMissing(ILogger<AuthenticationStepResolver> logger);

    /// <summary>No step instance was supplied.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The name it would have been registered under.</param>
    [MessageLogging(EventId = 91153, Level = LogLevel.Error,
        Message = "A step must be supplied to register '{stepName}'")]
    internal static partial IGenericMessage StepMissing(
        ILogger<AuthenticationStepResolver> logger, string stepName);

    /// <summary>Two packages claim the same step name.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The contested name.</param>
    /// <param name="existing">What holds it.</param>
    /// <param name="attempted">What tried to take it.</param>
    // Why Error and not last-wins: whichever won would depend on assembly load order, so the same
    // flow would mean different things on different hosts. Refusing makes the collision visible.
    [MessageLogging(EventId = 91154, Level = LogLevel.Error,
        Message = "Step '{stepName}' is already registered as {existing}; {attempted} cannot take the same name")]
    internal static partial IGenericMessage AlreadyRegistered(
        ILogger<AuthenticationStepResolver> logger, string stepName, string existing, string attempted);

    /// <summary>A flow named a step nothing registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The name the flow used.</param>
    /// <param name="known">What is registered.</param>
    // Why the known names are included: the usual cause is a package reference that was removed, and
    // the list turns "why does this flow not work" into "that package is gone" without a debugger.
    [MessageLogging(EventId = 91155, Level = LogLevel.Error,
        Message = "No step is registered as '{stepName}'. Registered: {known}")]
    internal static partial IGenericMessage NotRegistered(
        ILogger<AuthenticationStepResolver> logger, string stepName, string known);

    /// <summary>A flow orders a step before something it requires.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step that could not run.</param>
    /// <param name="missing">What no earlier step contributes.</param>
    [MessageLogging(EventId = 91156, Level = LogLevel.Error,
        Message = "Flow '{flowName}' orders '{stepName}' before {missing} is contributed by any earlier step")]
    internal static partial IGenericMessage OrderInvalid(
        ILogger<AuthenticationStepResolver> logger, string flowName, string stepName, string missing);
}
