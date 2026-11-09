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
}
