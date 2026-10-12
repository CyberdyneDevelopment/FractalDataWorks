using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>MessageLogging for <see cref="FieldMappingTransformationResolver"/> and the chains it resolves.</summary>
[MessageLoggingTypeCode("DATA")]
internal static partial class FieldMappingTransformationResolverLog
{
    /// <summary>A source's attached transformation chains were resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sourceName">The data-set source.</param>
    /// <param name="fieldCount">How many fields carry a chain.</param>
    [MessageLogging(EventId = 11570, Level = LogLevel.Trace,
        Message = "Resolved transformation chains for {fieldCount} field(s) of source '{sourceName}'")]
    internal static partial IGenericMessage Resolved(ILogger logger, string sourceName, int fieldCount);

    /// <summary>Attached transformations were applied to a read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fieldCount">How many fields were transformed.</param>
    /// <param name="rowCount">How many rows were read.</param>
    /// <param name="sourceName">The data-set source.</param>
    [MessageLogging(EventId = 11571, Level = LogLevel.Debug,
        Message = "Applied transformation chains to {fieldCount} field(s) across {rowCount} row(s) from source '{sourceName}'")]
    internal static partial IGenericMessage Applied(ILogger logger, int fieldCount, int rowCount, string sourceName);

    /// <summary>A field mapping is attached to a transformation that no longer exists.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fieldName">The logical field.</param>
    /// <param name="transformationId">The attached transformation.</param>
    [MessageLogging(EventId = 31570, Level = LogLevel.Error,
        Message = "Field '{fieldName}' is attached to transformation {transformationId}, which cannot be found")]
    internal static partial IGenericMessage TransformationMissing(ILogger logger, string fieldName, Guid transformationId);

    /// <summary>A field mapping is attached to a transformation that is not a Field transformation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fieldName">The logical field.</param>
    /// <param name="transformationName">The transformation.</param>
    /// <param name="implementation">The kind it is.</param>
    [MessageLogging(EventId = 61570, Level = LogLevel.Error,
        Message = "Field '{fieldName}' is attached to transformation '{transformationName}', a {implementation} transformation; only Field transformations attach to field mappings")]
    internal static partial IGenericMessage TransformationNotFieldLevel(ILogger logger, string fieldName, string transformationName, string implementation);

    /// <summary>An attached transformation names a type that is not a per-field operation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformationName">The transformation.</param>
    /// <param name="transformationType">The type it names.</param>
    [MessageLogging(EventId = 31571, Level = LogLevel.Error,
        Message = "Transformation '{transformationName}' names '{transformationType}', which is not a registered per-field operation")]
    internal static partial IGenericMessage OperationUnknown(ILogger logger, string transformationName, string transformationType);

    /// <summary>A field mapping's attachments could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sourceName">The data-set source.</param>
    /// <param name="fieldName">The logical field.</param>
    [MessageLogging(EventId = 71570, Level = LogLevel.Error,
        Message = "The transformations attached to field '{fieldName}' of source '{sourceName}' could not be read")]
    internal static partial IGenericMessage AttachmentsUnreadable(ILogger logger, string sourceName, string fieldName);

    /// <summary>A transformation failed on a value during a read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformationName">The transformation.</param>
    /// <param name="fieldName">The logical field.</param>
    [MessageLogging(EventId = 91570, Level = LogLevel.Error,
        Message = "Transformation '{transformationName}' failed on field '{fieldName}'")]
    internal static partial IGenericMessage TransformFailed(ILogger logger, string transformationName, string fieldName);
}
