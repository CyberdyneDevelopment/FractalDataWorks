using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>Messages of the registration of the permission, role and organization role providers, which write each description in the unit of the write (FDW-6208).</summary>
[MessageLoggingTypeCode("AUTHZDESC")]
public static partial class AuthorizationDescriptionsRegistrationLog
{
    /// <summary>Logs that the host registered the authorization providers but nothing registered the descriptions writer they write each description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60382, Level = LogLevel.Critical,
        Message = "The authorization domain is registered but nothing registered IDescriptionWriter, which it writes each permission's, role's and organization role's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The authorization domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
