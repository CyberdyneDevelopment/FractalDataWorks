using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Logging;

/// <summary>Messages of the registration of the notification and notification rule providers, which write each description in the unit of the write (FDW-7203).</summary>
[MessageLoggingTypeCode("NOTIFICATION")]
public static partial class NotificationDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the notification domain but nothing registered the descriptions writer its providers write each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60346, Level = LogLevel.Critical,
        Message = "The notification domain is registered but nothing registered IDescriptionWriter, which it writes each notification's and rule's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The notification domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
