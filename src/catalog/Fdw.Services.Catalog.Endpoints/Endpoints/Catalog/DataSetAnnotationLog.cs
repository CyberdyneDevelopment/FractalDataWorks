using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// MessageLogging for resolving a DataSet annotation (FDW-3047).
/// EventId ranges: 22750 (the caller has no user id, 400), 42750 (the annotation is already resolved, 409).
/// </summary>
[MessageLoggingTypeCode("CATALOG")]
public static partial class DataSetAnnotationLog
{
    /// <summary>Logs a resolve by a caller whose token carries no user id.</summary>
    [MessageLogging(EventId = 22750, Level = LogLevel.Warning,
        Message = "Annotation '{annotationId}' cannot be resolved: the caller's token carries no user id",
        ClientMessage = "An annotation is resolved by a person; the token carries no user id")]
    public static partial IGenericMessage ResolverUnknown(ILogger logger, string annotationId);

    /// <summary>Logs a resolve of an annotation that is already resolved.</summary>
    [MessageLogging(EventId = 42750, Level = LogLevel.Warning,
        Message = "Annotation '{annotationId}' is already resolved",
        ClientMessage = "The annotation is already resolved")]
    public static partial IGenericMessage AlreadyResolved(ILogger logger, string annotationId);
}
