using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>Messages of the registration of the authentication flow provider, which writes each flow's description in the unit of the write (FDW-6208).</summary>
[MessageLoggingTypeCode("AUTHFLOWDESC")]
public static partial class AuthenticationFlowDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the authentication steps but nothing registered the descriptions writer the flow provider writes each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60384, Level = LogLevel.Critical,
        Message = "The authentication flows are registered but nothing registered IDescriptionWriter, which the flow provider writes each flow's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The authentication flows need the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
