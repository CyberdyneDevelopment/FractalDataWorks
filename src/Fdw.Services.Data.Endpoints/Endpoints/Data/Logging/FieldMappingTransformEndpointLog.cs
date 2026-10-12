using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for the transformation endpoints: the standalone <c>/transforms</c> resource and
/// the transformations attached to a field mapping (FDW-846).
/// </summary>
/// <remarks>
/// EventIds follow the result categories (first digit of a five-digit id): 1 progress, 2 validation
/// (400), 3 missing (404), 4 conflict (409), 7 dependency.
/// </remarks>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class FieldMappingTransformEndpointLog
{
    /// <summary>Logs that a field mapping's transformations are being listed.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace, Message = "Listing transforms")]
    public static partial IGenericMessage ListingTransforms(ILogger logger);

    /// <summary>Logs how many transformations a field mapping has.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug, Message = "Listed {count} transforms")]
    public static partial IGenericMessage ListedTransforms(ILogger logger, int count);

    /// <summary>Logs that listing a field mapping's transformations failed.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning, Message = "Failed to list transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ListTransformsFailed(ILogger logger, Guid fieldMappingId);

    /// <summary>Logs that a transformation is being attached to a field mapping.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "Attaching transformation '{transformationName}'")]
    public static partial IGenericMessage SavingTransform(ILogger logger, string transformationName);

    /// <summary>Logs that a transformation was attached to a field mapping.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information, Message = "Attached transformation '{transformationName}'")]
    public static partial IGenericMessage SavedTransform(ILogger logger, string transformationName);

    /// <summary>Logs that attaching a transformation failed.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning, Message = "Failed to attach transformation '{transformationName}'")]
    public static partial IGenericMessage SaveTransformFailed(ILogger logger, string transformationName);

    /// <summary>Logs that a transformation is being detached from a field mapping.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Detaching transformation '{transformationName}'")]
    public static partial IGenericMessage DeletingTransform(ILogger logger, string transformationName);

    /// <summary>Logs that a transformation was detached from a field mapping.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information, Message = "Detached transformation '{transformationName}'")]
    public static partial IGenericMessage DeletedTransform(ILogger logger, string transformationName);

    /// <summary>Logs that detaching a transformation failed.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning, Message = "Failed to detach transformation '{transformationName}'")]
    public static partial IGenericMessage DeleteTransformFailed(ILogger logger, string transformationName);

    /// <summary>Logs that a field mapping's chain is being reordered.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Reordering transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ReorderingTransforms(ILogger logger, Guid fieldMappingId);

    /// <summary>Logs that a field mapping's chain was reordered.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information, Message = "Reordered transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ReorderedTransforms(ILogger logger, Guid fieldMappingId);

    /// <summary>Logs that reordering a field mapping's chain failed.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning, Message = "Failed to reorder transforms for field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage ReorderTransformsFailed(ILogger logger, Guid fieldMappingId);

    /// <summary>Logs that the transform types are being listed.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Listing transform types")]
    public static partial IGenericMessage ListingTransformTypes(ILogger logger);

    /// <summary>Logs how many transform types there are.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Debug, Message = "Listed {count} transform types")]
    public static partial IGenericMessage ListedTransformTypes(ILogger logger, int count);

    /// <summary>Logs that the transformations are being listed.</summary>
    [MessageLogging(EventId = 11540, Level = LogLevel.Trace, Message = "Listing transformations")]
    public static partial IGenericMessage ListingTransformations(ILogger logger);

    /// <summary>Logs that a transformation was saved.</summary>
    [MessageLogging(EventId = 11541, Level = LogLevel.Information, Message = "Saved transformation '{name}' ({transformationType})")]
    public static partial IGenericMessage TransformationSaved(ILogger logger, string name, string transformationType);

    /// <summary>Logs that a transformation was deleted.</summary>
    [MessageLogging(EventId = 11542, Level = LogLevel.Information, Message = "Deleted transformation '{name}'")]
    public static partial IGenericMessage TransformationDeleted(ILogger logger, string name);

    /// <summary>Logs that a transformation named an operation that is not registered.</summary>
    [MessageLogging(EventId = 21540, Level = LogLevel.Warning, Message = "Unknown transformation type '{transformationType}'; see /transform-types")]
    public static partial IGenericMessage TransformationTypeUnknown(ILogger logger, string transformationType);

    /// <summary>Logs that a required parameter of the operation was not given.</summary>
    [MessageLogging(EventId = 21541, Level = LogLevel.Warning, Message = "Transformation type '{transformationType}' requires parameter '{parameter}'")]
    public static partial IGenericMessage TransformationParameterMissing(ILogger logger, string transformationType, string parameter);

    /// <summary>Logs that a parameter the operation does not declare was given.</summary>
    [MessageLogging(EventId = 21542, Level = LogLevel.Warning, Message = "Transformation type '{transformationType}' has no parameter '{parameter}'")]
    public static partial IGenericMessage TransformationParameterUnknown(ILogger logger, string transformationType, string parameter);

    /// <summary>Logs that a reorder did not name every attached transformation exactly once.</summary>
    [MessageLogging(EventId = 21543, Level = LogLevel.Warning, Message = "Reorder for field mapping '{fieldMappingId}' must name every attached transformation exactly once")]
    public static partial IGenericMessage ReorderSetMismatch(ILogger logger, Guid fieldMappingId);

    /// <summary>Logs that a field mapping id in the route could not be read.</summary>
    [MessageLogging(EventId = 21544, Level = LogLevel.Warning, Message = "Field mapping id '{fieldMappingId}' is not a valid id")]
    public static partial IGenericMessage FieldMappingIdInvalid(ILogger logger, string fieldMappingId);

    /// <summary>Logs that a transformation of a set kind was given where only a Field transformation fits.</summary>
    [MessageLogging(EventId = 21545, Level = LogLevel.Warning, Message = "Transformation '{name}' is a {implementation} transformation; only a Field transformation can be used here")]
    public static partial IGenericMessage TransformationNotFieldLevel(ILogger logger, string name, string implementation);

    /// <summary>Logs that no transformation has the given name.</summary>
    [MessageLogging(EventId = 31540, Level = LogLevel.Warning, Message = "No transformation named '{name}'")]
    public static partial IGenericMessage TransformationNotFound(ILogger logger, string name);

    /// <summary>Logs that the transformation is not attached to the field mapping.</summary>
    [MessageLogging(EventId = 31541, Level = LogLevel.Warning, Message = "Transformation '{name}' is not attached to field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage TransformationNotAttached(ILogger logger, Guid fieldMappingId, string name);

    /// <summary>Logs a refused change to a seeded standard transformation.</summary>
    [MessageLogging(EventId = 41540, Level = LogLevel.Warning, Message = "Transformation '{name}' is system-provided and cannot be changed or deleted; define your own instead")]
    public static partial IGenericMessage SystemProvidedTransformationImmutable(ILogger logger, string name);

    /// <summary>Logs a refused delete of a transformation still in use.</summary>
    [MessageLogging(EventId = 41541, Level = LogLevel.Warning, Message = "Transformation '{name}' is attached to {count} field mapping(s); detach it first")]
    public static partial IGenericMessage TransformationStillAttached(ILogger logger, string name, int count);

    /// <summary>Logs a refused second attachment of the same transformation.</summary>
    [MessageLogging(EventId = 41542, Level = LogLevel.Warning, Message = "Transformation '{name}' is already attached to field mapping '{fieldMappingId}'")]
    public static partial IGenericMessage TransformationAlreadyAttached(ILogger logger, Guid fieldMappingId, string name);
}
