using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>Messages of the registration of the secret manager provider, which writes each secret manager's description in the unit of the write (FDW-6208).</summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class SecretManagerDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the secret manager provider but nothing registered the descriptions writer it writes each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60391, Level = LogLevel.Critical,
        Message = "The secret manager domain is registered but nothing registered IDescriptionWriter, which it writes each secret manager's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The secret manager domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
