using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>Messages for <see cref="StandardAcrPolicy"/>.</summary>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class AcrPolicyLog
{
    /// <summary>
    /// A level was named -- by a flow's MinimumAcr, or reached and then looked up -- that no declared
    /// assurance level answers to. The flow is misconfigured, or the host references no package that
    /// declares the deployment's levels (FDW-954).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="acrValue">The level named.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60004, Level = LogLevel.Error,
        Message = "Assurance level '{acrValue}' is not declared: no AssuranceLevels option answers to it")]
    internal static partial IGenericMessage LevelNotDeclared(ILogger<StandardAcrPolicy> logger, string acrValue);
}
