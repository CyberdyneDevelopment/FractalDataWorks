using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Logging;

/// <summary>Messages of the registration of the messaging provider, which writes each description in the unit of the write (FDW-7203).</summary>
[MessageLoggingTypeCode("MESSAGING")]
public static partial class MessagingDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the messaging domain but nothing registered the descriptions writer its provider writes each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60347, Level = LogLevel.Critical,
        Message = "The messaging domain is registered but nothing registered IDescriptionWriter, which it writes each messaging configuration's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The messaging domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
