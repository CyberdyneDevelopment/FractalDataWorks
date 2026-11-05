using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// MessageLogging for resolving a DataSet annotation (FDW-3047).
/// EventId ranges: 22750 (the caller has no user id, 400), 42750 (the annotation is already resolved, 409), 22755 (the caller acts in no tenant, 400), 32755 (the annotation's data set is not there, 404).
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

    /// <summary>Logs a write of an annotation's tags by a caller who acts in no tenant.</summary>
    [MessageLogging(EventId = 22755, Level = LogLevel.Warning,
        Message = "The tags of DataSet '{dataSetName}' cannot be written: the caller acts in no tenant",
        ClientMessage = "Tags are made in a tenant; the token carries no active tenant")]
    public static partial IGenericMessage TagsCallerHasNoTenant(ILogger logger, string dataSetName);

    /// <summary>Logs a read or write of the tags of an annotation whose data set is not there.</summary>
    [MessageLogging(EventId = 32755, Level = LogLevel.Warning,
        Message = "The tags of DataSet '{dataSetName}' cannot be read or written: no data set has that name",
        ClientMessage = "No data set has the name the annotation gives")]
    public static partial IGenericMessage TagsDataSetNotFound(ILogger logger, string dataSetName);
}
