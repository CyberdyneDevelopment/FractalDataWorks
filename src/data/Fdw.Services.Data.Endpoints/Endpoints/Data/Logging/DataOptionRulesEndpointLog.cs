using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for the data option rule routes (FDW-6339).</summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataOptionRulesEndpointLog
{
    /// <summary>A route of the stage 1 contract (FDW-6348) was called before stage 2 built it. Category 3: the route's work is not there yet.</summary>
    [MessageLogging(EventId = 31700, Level = LogLevel.Warning, Message = "The data option route '{route}' was called but is not built yet",
        ClientMessage = "The route {route} is declared but not built yet.")]
    public static partial IGenericMessage RouteNotBuilt(ILogger logger, string route);

    /// <summary>The request has no body where the route needs one (FDW-6489). Category 2: the request is not one the route can answer.</summary>
    [MessageLogging(EventId = 21700, Level = LogLevel.Warning, Message = "The data option route '{route}' was called without its '{body}'",
        ClientMessage = "The request needs a {body}.")]
    public static partial IGenericMessage RuleBodyMissing(ILogger logger, string route, string body);

    /// <summary>A field of the body that must be the route's names another (FDW-6489).</summary>
    [MessageLogging(EventId = 21701, Level = LogLevel.Warning, Message = "The body's {field} '{body}' is not the route's '{route}'",
        ClientMessage = "The rule's {field} must be {route}, the route's, not {body}.")]
    public static partial IGenericMessage RuleBodyTargetDiffers(ILogger logger, string field, string body, string route);

    /// <summary>A field the route needs and the body leaves out, where the service has no code of its own for it (FDW-6489).</summary>
    [MessageLogging(EventId = 21703, Level = LogLevel.Warning, Message = "The data option route '{route}' needs '{field}' and the body leaves it out",
        ClientMessage = "The request needs {field}.")]
    public static partial IGenericMessage RuleFieldMissing(ILogger logger, string route, string field);
}
