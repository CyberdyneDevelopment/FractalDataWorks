using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>Messages of the registration of the server setting provider, which writes each setting's description in the unit of the write (FDW-6208).</summary>
[MessageLoggingTypeCode("SETTINGSDESC")]
public static partial class SettingsDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the settings collection but nothing registered the descriptions writer the server setting provider writes each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60383, Level = LogLevel.Critical,
        Message = "The settings domain is registered but nothing registered IDescriptionWriter, which the server setting provider writes each setting's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The settings domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
