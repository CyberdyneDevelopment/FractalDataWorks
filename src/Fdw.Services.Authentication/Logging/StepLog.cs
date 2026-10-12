using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging shared across every step, regardless of which package declares it.
/// </summary>
/// <remarks>
/// EventId range: 91143. The methods that used to live here for the steps shipped with the platform
/// moved with those steps to their own <c>.Registration</c> packages in reference-servicetypes
/// (FDW-848) — this is what remains, because <see cref="Fdw.Services.Authentication.Flow.AuthenticationStepTypeBase{TService,TFactory}.NotInitialized"/>
/// needs one code every step reports the same way, and a step in another assembly cannot reach an
/// internal log to declare its own copy.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class StepLog
{
    /// <summary>Logs a step asked to run before its Initialize captured what it needs.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step the flow named.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91143, Level = LogLevel.Error,
        Message = "Step '{stepName}' ran before its dependencies were captured, so it cannot do its work. The option's Initialize phase did not run, which means the host was not fully initialized before a flow reached this step")]
    internal static partial IGenericMessage NotInitialized(ILogger logger, string stepName);
}
