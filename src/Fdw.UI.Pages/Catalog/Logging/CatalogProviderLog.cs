using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Components.Logging;

/// <summary>
/// MessageLogging for CatalogProvider operations.
/// EventId range: 4490-4504
/// </summary>
[MessageLoggingTypeCode("COMPONENTS7")]
public static partial class CatalogProviderLog
{
    /// <summary>
    /// Logs that the catalog provider has started loading catalog data.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "CatalogProvider: Loading catalog data")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    /// <summary>
    /// Logs that the catalog provider finished loading the given number of datasets.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataSetCount">The number of datasets that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "CatalogProvider: Loaded {dataSetCount} datasets")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int dataSetCount);

    /// <summary>
    /// Logs that the catalog provider failed to load catalog data.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "CatalogProvider: Failed to load catalog data")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while the catalog provider was loading catalog data.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while loading catalog data.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "CatalogProvider: Exception loading catalog data")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the catalog provider is searching the catalog for the given query.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="query">The search query being executed against the catalog.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "CatalogProvider: Searching catalog for '{query}'")]
    public static partial IGenericMessage Searching(ILogger logger, string query);

    /// <summary>
    /// Logs that a catalog search returned the given number of results.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="count">The number of results returned by the search.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "CatalogProvider: Search returned {count} results")]
    public static partial IGenericMessage SearchCompleted(ILogger logger, int count);

    /// <summary>
    /// Logs that a dataset with the given name was selected in the catalog.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the dataset that was selected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "CatalogProvider: DataSet '{name}' selected")]
    public static partial IGenericMessage DataSetSelected(ILogger logger, string name);

    /// <summary>
    /// Logs that the catalog provider is refreshing catalog data.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "CatalogProvider: Refreshing catalog data")]
    public static partial IGenericMessage Refreshing(ILogger logger);

    /// <summary>
    /// Logs that the Pipeline Builder was started from the catalog with the named DataSet as source.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataSetName">The name of the DataSet that was selected as the source.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "CatalogProvider: Starting Pipeline Builder with source DataSet '{dataSetName}'")]
    public static partial IGenericMessage StartBuilderFromCatalog(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs that a derived DataSet wizard was started from the catalog with the named DataSet as base.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataSetName">The name of the DataSet that was selected as the derivation base.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "CatalogProvider: Deriving new DataSet from '{dataSetName}'")]
    public static partial IGenericMessage DeriveDataSetFromCatalog(ILogger logger, string dataSetName);
}
