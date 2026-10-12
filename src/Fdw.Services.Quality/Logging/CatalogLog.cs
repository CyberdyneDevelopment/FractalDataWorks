using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Quality.Logging;

/// <summary>
/// MessageLogging methods for Catalog operations.
/// Every log message is returned in the result AND logged.
/// EventId range: 8500-8549
/// </summary>
[MessageLoggingTypeCode("QUALITY")]
public static partial class CatalogLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Glossary Events (8500-8509)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs creation of a glossary term.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "Glossary term '{termName}' created in category '{category}'")]
    public static partial IGenericMessage TermCreated(ILogger logger, string termName, string category);

    /// <summary>Logs update of a glossary term.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Glossary term '{termName}' updated")]
    public static partial IGenericMessage TermUpdated(ILogger logger, string termName);

    /// <summary>Logs deletion of a glossary term.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Glossary term '{termName}' deleted")]
    public static partial IGenericMessage TermDeleted(ILogger logger, string termName);

    /// <summary>Logs a glossary search operation.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Searching glossary for '{query}' in category '{category}'")]
    public static partial IGenericMessage SearchingTerms(ILogger logger, string query, string? category);

    // ═══════════════════════════════════════════════════════════════════════════
    // Annotation Events (8510-8519)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs annotation of a data set.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "DataSet '{dataSetName}' annotated by '{owner}'")]
    public static partial IGenericMessage DataSetAnnotated(ILogger logger, string dataSetName, string owner);

    /// <summary>Logs update of an annotation.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Annotation updated for DataSet '{dataSetName}'")]
    public static partial IGenericMessage AnnotationUpdated(ILogger logger, string dataSetName);

    /// <summary>Logs loading an annotation for a data set.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "Loading annotation for DataSet '{dataSetName}'")]
    public static partial IGenericMessage LoadingAnnotation(ILogger logger, string dataSetName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Search Events (8520-8529)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs completion of a catalog search.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "Catalog search returned {resultCount} results for query '{query}'")]
    public static partial IGenericMessage SearchCompleted(ILogger logger, string query, int resultCount);

    /// <summary>Logs indexing a data set for the catalog.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug,
        Message = "Indexing catalog for DataSet '{dataSetName}'")]
    public static partial IGenericMessage IndexingDataSet(ILogger logger, string dataSetName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Events (8540-8549)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a glossary term was not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error,
        Message = "Glossary term not found: '{termId}'")]
    public static partial IGenericMessage TermNotFound(ILogger logger, Guid termId);

    /// <summary>Logs a duplicate glossary term name.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Error,
        Message = "Duplicate glossary term name: '{termName}'")]
    public static partial IGenericMessage DuplicateTermName(ILogger logger, string termName);

    /// <summary>Logs a failed catalog operation.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Catalog operation '{operation}' failed")]
    public static partial IGenericMessage OperationFailed(ILogger logger, Exception exception, string operation);

    /// <summary>Logs that an annotation was not found.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "Annotation not found for DataSet '{dataSetName}'")]
    public static partial IGenericMessage AnnotationNotFound(ILogger logger, string dataSetName);
}
