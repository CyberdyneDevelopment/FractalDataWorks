using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for field mapping transform endpoint base operations.
/// EventId range: 4240-4260
/// </summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class FieldMappingTransformEndpointLog
{
    // List transforms (4240-4242)

    /// <summary>Logs the start of listing transforms for a field mapping.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace, Message = "Listing transforms")]
    public static partial IGenericMessage ListingTransforms(ILogger logger);

    /// <summary>Logs the count of transforms returned.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug, Message = "Listed {count} transforms")]
    public static partial IGenericMessage ListedTransforms(ILogger logger, int count);

    /// <summary>Logs a failure listing transforms for a field mapping.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning, Message = "Failed to list transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ListTransformsFailed(ILogger logger, Guid fieldMappingId);

    // Save transform (4243-4245)

    /// <summary>Logs the start of saving a transform.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "Saving transform of type '{transformType}'")]
    public static partial IGenericMessage SavingTransform(ILogger logger, string transformType);

    /// <summary>Logs successful transform save.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information, Message = "Saved transform of type '{transformType}'")]
    public static partial IGenericMessage SavedTransform(ILogger logger, string transformType);

    /// <summary>Logs a failure saving a transform.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning, Message = "Failed to save transform of type '{transformType}'")]
    public static partial IGenericMessage SaveTransformFailed(ILogger logger, string transformType);

    // Delete transform (4246-4248)

    /// <summary>Logs the start of deleting a transform.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Deleting transform '{transformId}'")]
    public static partial IGenericMessage DeletingTransform(ILogger logger, Guid transformId);

    /// <summary>Logs successful transform deletion.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information, Message = "Deleted transform '{transformId}'")]
    public static partial IGenericMessage DeletedTransform(ILogger logger, Guid transformId);

    /// <summary>Logs a failure deleting a transform.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning, Message = "Failed to delete transform '{transformId}'")]
    public static partial IGenericMessage DeleteTransformFailed(ILogger logger, Guid transformId);

    // Reorder transforms (4249-4251)

    /// <summary>Logs the start of reordering transforms for a field mapping.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Reordering transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ReorderingTransforms(ILogger logger, Guid fieldMappingId);

    /// <summary>Logs successful transform reorder.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information, Message = "Reordered transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ReorderedTransforms(ILogger logger, Guid fieldMappingId);

    /// <summary>Logs a failure reordering transforms.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning, Message = "Failed to reorder transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ReorderTransformsFailed(ILogger logger, Guid fieldMappingId);

    // List transform types (4252-4253)

    /// <summary>Logs the start of listing available transform types.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Listing transform types")]
    public static partial IGenericMessage ListingTransformTypes(ILogger logger);

    /// <summary>Logs the count of transform types returned.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Debug, Message = "Listed {count} transform types")]
    public static partial IGenericMessage ListedTransformTypes(ILogger logger, int count);

    // Not found (4254)

    /// <summary>Logs that a transform was not found.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "Transform '{transformId}' not found")]
    public static partial IGenericMessage TransformNotFound(ILogger logger, Guid transformId);
}
