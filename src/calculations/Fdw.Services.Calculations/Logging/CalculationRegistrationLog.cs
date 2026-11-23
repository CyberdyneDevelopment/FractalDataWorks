using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>Messages of the calculation service type's registration (FDW-6208).</summary>
[MessageLoggingTypeCode("CALCULATIONS")]
public static partial class CalculationRegistrationLog
{
    /// <summary>Logs that the host registered the calculation service type but nothing registered the descriptions writer it writes each calculation's and version's description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60344, Level = LogLevel.Critical,
        Message = "The calculation domain is registered but nothing registered IDescriptionWriter, which it writes each calculation's and calculation version's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The calculation domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
