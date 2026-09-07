using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Logging;

/// <summary>
/// MessageLogging for resolving an API client's base address from the HTTP connection of the same
/// name. EventId range: 11020-11021 (trace), 61010-61011 (warning).
/// </summary>
[MessageLoggingTypeCode("HTTP")]
public static partial class HttpApiEndpointSourceLog
{
    /// <summary>Logs that a client's endpoint was resolved from its connection.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clientName">The API client name.</param>
    /// <param name="baseUrl">The resolved base URL.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11020, Level = LogLevel.Debug,
        Message = "API client '{clientName}' resolved to {baseUrl} from the HTTP connection of the same name")]
    public static partial IGenericMessage EndpointResolved(ILogger logger, string clientName, string baseUrl);

    /// <summary>Logs that no connection is declared under a client's name.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clientName">The API client name.</param>
    /// <returns>The message.</returns>
    /// <remarks>
    /// Debug rather than Warning: a host registers a client for every package it references and
    /// calls only a few, so most clients having no connection is the ordinary case and not a
    /// problem. It becomes one only when something actually tries to send, and the caller reports
    /// that through EndpointNotDeclared.
    /// </remarks>
    [MessageLogging(EventId = 11021, Level = LogLevel.Debug,
        Message = "No connection is declared under the name '{clientName}', so no endpoint was resolved for it")]
    public static partial IGenericMessage NoConnectionForClient(ILogger logger, string clientName);

    /// <summary>Logs that the connection of a client's name is not an HTTP connection.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clientName">The API client name.</param>
    /// <param name="actualType">The implementation type actually declared.</param>
    /// <returns>The message.</returns>
    /// <remarks>
    /// Warning rather than Debug: unlike an absent connection, a present one of the wrong kind is
    /// a name collision between two things that mean different things, and whichever one was
    /// intended, the other is now broken.
    /// </remarks>
    [MessageLogging(EventId = 61010, Level = LogLevel.Warning,
        Message = "The connection named '{clientName}' is a {actualType}, not an HTTP connection, so it declares no endpoint for that API client")]
    public static partial IGenericMessage ConnectionIsNotHttp(ILogger logger, string clientName, string actualType);

    /// <summary>Logs that a client's HTTP connection declares no base URL.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clientName">The API client name.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 61011, Level = LogLevel.Warning,
        Message = "The HTTP connection named '{clientName}' declares an empty BaseUrl, so the API client of that name still has nowhere to send")]
    public static partial IGenericMessage ConnectionHasNoBaseUrl(ILogger logger, string clientName);
}
