using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for the option-row routes (FDW-5664).</summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataOptionRowsEndpointLog
{
    /// <summary>A route value the option-row list is served under did not reach it: the route was not the one it declares.</summary>
    [MessageLogging(EventId = 91670, Level = LogLevel.Error, Message = "The option rows list was reached without its route value '{name}'")]
    public static partial IGenericMessage RouteValueMissing(ILogger logger, string name);
}
