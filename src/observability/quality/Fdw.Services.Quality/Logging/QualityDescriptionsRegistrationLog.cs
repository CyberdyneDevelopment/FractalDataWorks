using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Logging;

/// <summary>Messages of the registration of the quality rule and data set annotation providers, which write each description in the unit of the write (FDW-6208).</summary>
[MessageLoggingTypeCode("QUALITY")]
public static partial class QualityDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the quality and catalog providers but nothing registered the descriptions writer they write each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60349, Level = LogLevel.Critical,
        Message = "The quality and catalog domains are registered but nothing registered IDescriptionWriter, which they write each quality rule's, reference value's, annotation's and field description's text with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The quality and catalog domains need the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
