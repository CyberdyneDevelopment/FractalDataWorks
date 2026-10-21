using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Documents.Abstractions.Logging;

/// <summary>
/// MessageLogging for the anticipated failures of document storage (FDW-1500): each logs and is returned in the failure result,
/// so a caller returns the failure instead of catching a storage exception.
/// EventId range: 22100-22109.
/// </summary>
[MessageLoggingTypeCode("DOCUMENTS")]
public static partial class DocumentLog
{
    /// <summary>Logs a path that is not a document path.</summary>
    [MessageLogging(EventId = 22100, Level = LogLevel.Warning, Message = "The path '{path}' is not a document path: {reason}")]
    public static partial IGenericMessage PathInvalid(ILogger logger, string path, string reason);

    /// <summary>Logs a path outside the scope of the connection.</summary>
    [MessageLogging(EventId = 22101, Level = LogLevel.Warning, Message = "The path '{path}' is outside the scope '{scope}' of the connection")]
    public static partial IGenericMessage OutsideScope(ILogger logger, string path, string scope);

    /// <summary>Logs a document that is not there.</summary>
    [MessageLogging(EventId = 22102, Level = LogLevel.Information, Message = "There is no document at '{path}'")]
    public static partial IGenericMessage NotFound(ILogger logger, string path);

    /// <summary>Logs a create-only write onto a document that exists.</summary>
    [MessageLogging(EventId = 22103, Level = LogLevel.Information, Message = "A document already exists at '{path}' and the write is create-only")]
    public static partial IGenericMessage AlreadyExists(ILogger logger, string path);

    /// <summary>Logs a document larger than the connection takes.</summary>
    [MessageLogging(EventId = 22104, Level = LogLevel.Warning, Message = "The document at '{path}' is {length} bytes; the most the connection takes is {limit}")]
    public static partial IGenericMessage TooLarge(ILogger logger, string path, long length, long limit);

    /// <summary>Logs a content type the connection does not take.</summary>
    [MessageLogging(EventId = 22105, Level = LogLevel.Warning, Message = "The content type '{contentType}' is not allowed for '{path}'")]
    public static partial IGenericMessage ContentTypeNotAllowed(ILogger logger, string path, string contentType);

    /// <summary>Logs a read link asked of a connection that creates none.</summary>
    [MessageLogging(EventId = 22106, Level = LogLevel.Warning, Message = "The connection '{connection}' does not create temporary read links")]
    public static partial IGenericMessage ReadLinksNotSupported(ILogger logger, string connection);

    /// <summary>Logs a request the storage refused.</summary>
    [MessageLogging(EventId = 22107, Level = LogLevel.Error, Message = "The storage refused the request for '{path}': {reason}")]
    public static partial IGenericMessage StorageRefused(ILogger logger, string path, string reason);
}
