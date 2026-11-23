using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>Messages of the registration of the data set, data store, category and transformation providers, which write each description in the unit of the write (FDW-6121, FDW-6208, FDW-6815).</summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the data set, data store, category and transformation providers but nothing registered the descriptions writer they write each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60342, Level = LogLevel.Critical,
        Message = "The data set, data store, category and transformation domains are registered but nothing registered IDescriptionWriter, which they write each data set's, field's, data store's, category's and transformation's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The data set, data store, category and transformation domains need the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
