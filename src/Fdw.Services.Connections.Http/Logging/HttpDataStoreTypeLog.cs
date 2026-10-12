using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Logging;

/// <summary>
/// MessageLogging for the HTTP data-store type registration.
/// EventId range: 9630-9639
/// </summary>
[MessageLoggingTypeCode("HTTP")]
public static partial class HttpDataStoreTypeLog
{
    /// <summary>
    /// Logs that the HTTP data-store type registration completed.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataStoreTypeName">The data-store type name that was registered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "HTTP data-store type '{dataStoreTypeName}' registered (body-less header; generic builder)")]
    public static partial IGenericMessage RegistrationCompleted(ILogger logger, string dataStoreTypeName);
}
