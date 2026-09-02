using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Http.Authentication.Logging;

/// <summary>
/// MessageLogging for API client endpoint resolution.
/// EventId range: 60100-60109 (category 6 — Configuration / Setup).
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION2")]
public static partial class ApiEndpointLog
{
    /// <summary>
    /// Reports that a client was resolved but the host declares no endpoint for it, naming every key
    /// that would satisfy it.
    /// </summary>
    [MessageLogging(
        EventId = 60100,
        Level = LogLevel.Error,
        Message = "No endpoint is configured for API client '{clientName}', so its requests cannot be sent. Configure the client, or the HTTP connection named '{clientName}', in the configuration store.")]
    public static partial IGenericMessage EndpointNotDeclared(ILogger logger, string clientName);
}
