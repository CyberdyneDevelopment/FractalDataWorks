using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints;

/// <summary>The coded messages the webrestendpoints endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("WEBRESTANSWERS")]
public static partial class WebRestEndpointsAnswerLog
{

    /// <summary>Logs that The request to generic was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Warning, Message = "The request to generic was refused: the caller is not signed in", ClientMessage = "You must be signed in to generic")]
    public static partial IGenericMessage GenericNotSignedIn(ILogger logger);
}
