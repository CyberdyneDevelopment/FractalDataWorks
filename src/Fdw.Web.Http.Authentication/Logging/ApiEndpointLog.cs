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
    // Why the remediation is in the message rather than a doc comment: this is emitted at the moment a
    // client is resolved, in a host whose operator is reading logs and not source. The message names the
    // client and every key that would fix it, so the log line alone is actionable.
    [MessageLogging(
        EventId = 60100,
        Level = LogLevel.Error,
        Message = "No endpoint is declared for API client '{clientName}', so its requests cannot be sent. Declare ONE of: 'ApiClients:{clientName}:BaseUrl' (this host's endpoint for this one client), an HTTP connection named '{clientName}' in the configuration store, or 'ApiClients:BaseUrl' (this host's endpoint for all its API clients).")]
    public static partial IGenericMessage EndpointNotDeclared(ILogger logger, string clientName);
}
