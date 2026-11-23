using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>Messages of the workspace service type's registration (FDW-4556).</summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceRegistrationLog
{
    /// <summary>Logs that the workspace service type registered a service.</summary>
    [MessageLogging(EventId = 11705, Level = LogLevel.Trace, Message = "The workspace service type registered {serviceName}")]
    public static partial IGenericMessage Registered(ILogger logger, string serviceName);

    /// <summary>Logs that the host registered the workspace service type but nothing registered the descriptions writer it writes each workspace's description with.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60343, Level = LogLevel.Critical,
        Message = "The workspace domain is registered but nothing registered IDescriptionWriter, which it writes each workspace's description with in the unit of the write; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "The workspace domain needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);
}
