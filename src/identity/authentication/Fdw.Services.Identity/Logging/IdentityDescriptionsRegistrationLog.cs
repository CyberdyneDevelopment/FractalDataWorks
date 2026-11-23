using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Logging;

/// <summary>Messages of the registration of the identity provider, which writes each identity's description in the unit of the write (FDW-6208).</summary>
[MessageLoggingTypeCode("IDENTITY")]
public static partial class IdentityDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the identity provider but nothing registered the descriptions writer it writes each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60390, Level = LogLevel.Critical,
        Message = "The identity domain is registered but nothing registered IDescriptionWriter, which it writes each identity's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The identity domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
