using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Logging;

/// <summary>Messages of the registration of the escalation policy and threshold providers, which write each description in the unit of the write (FDW-7205).</summary>
[MessageLoggingTypeCode("OPERATIONS")]
public static partial class OperationsDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the operations service type but nothing registered the descriptions writer its escalation policy and threshold providers write each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60348, Level = LogLevel.Critical,
        Message = "The operations service type is registered but nothing registered IDescriptionWriter, which it writes each escalation policy's and threshold's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The operations service type needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
