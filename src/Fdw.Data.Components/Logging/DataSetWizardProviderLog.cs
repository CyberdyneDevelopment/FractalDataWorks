using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for DataSetWizardProvider operations.
/// EventId range: 4160-4179
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataSetWizardProviderLog
{
    /// <summary>
    /// Logs that the DataStore list is being loaded for the wizard.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11035,
        Level = LogLevel.Trace,
        Message = "Loading DataStore list for wizard")]
    public static partial IGenericMessage LoadingDataStores(ILogger logger);

    /// <summary>
    /// Logs that a number of DataStores were loaded for the wizard.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of DataStores that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11036,
        Level = LogLevel.Information,
        Message = "Loaded {count} DataStores for wizard")]
    public static partial IGenericMessage LoadedDataStores(ILogger logger, int count);

    /// <summary>
    /// Logs that loading the DataStore list for the wizard failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71035,
        Level = LogLevel.Warning,
        Message = "Failed to load DataStore list for wizard")]
    public static partial IGenericMessage LoadDataStoresFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception was thrown while loading the DataStore list for the wizard.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the DataStore list.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71036,
        Level = LogLevel.Error,
        Message = "Failed to load DataStore list for wizard")]
    public static partial IGenericMessage LoadDataStoresException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the named existing DataSet is being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the existing DataSet being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11037,
        Level = LogLevel.Trace,
        Message = "Loading existing DataSet '{name}'")]
    public static partial IGenericMessage LoadingExistingDataSet(ILogger logger, string name);

    /// <summary>
    /// Logs that the named existing DataSet was loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the existing DataSet that was loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11038,
        Level = LogLevel.Information,
        Message = "Loaded existing DataSet '{name}'")]
    public static partial IGenericMessage LoadedExistingDataSet(ILogger logger, string name);

    /// <summary>
    /// Logs that loading the named existing DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the existing DataSet that failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71037,
        Level = LogLevel.Warning,
        Message = "Failed to load existing DataSet '{name}'")]
    public static partial IGenericMessage LoadExistingDataSetFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception was thrown while loading the named existing DataSet.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the existing DataSet.</param>
    /// <param name="name">The name of the existing DataSet that failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71038,
        Level = LogLevel.Error,
        Message = "Failed to load existing DataSet '{name}'")]
    public static partial IGenericMessage LoadExistingDataSetException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that the containers for the named DataStore are being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore whose containers are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11039,
        Level = LogLevel.Trace,
        Message = "Loading containers for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadingContainersForDataStore(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs that a number of containers were loaded for the named DataStore.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of containers that were loaded.</param>
    /// <param name="dataStoreName">The name of the DataStore whose containers were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Debug,
        Message = "Loaded {count} containers for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadedContainersForDataStore(ILogger logger, int count, string dataStoreName);

    /// <summary>
    /// Logs that loading the containers for the named DataStore failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore whose containers failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71039,
        Level = LogLevel.Warning,
        Message = "Failed to load containers for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadContainersForDataStoreFailed(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs that an exception was thrown while loading the containers for the named DataStore.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the containers.</param>
    /// <param name="dataStoreName">The name of the DataStore whose containers failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71040,
        Level = LogLevel.Error,
        Message = "Failed to load containers for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadContainersForDataStoreException(ILogger logger, Exception exception, string dataStoreName);

    /// <summary>
    /// Logs that the named DataSet is being submitted.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataSet being submitted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Trace,
        Message = "Submitting DataSet '{name}'")]
    public static partial IGenericMessage SubmittingDataSet(ILogger logger, string name);

    /// <summary>
    /// Logs that the named DataSet was submitted successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataSet that was submitted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Information,
        Message = "DataSet '{name}' submitted successfully")]
    public static partial IGenericMessage DataSetSubmitted(ILogger logger, string name);

    /// <summary>
    /// Logs that submitting the named DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataSet that failed to submit.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71041,
        Level = LogLevel.Warning,
        Message = "Failed to submit DataSet '{name}'")]
    public static partial IGenericMessage SubmitDataSetFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception was thrown while submitting the named DataSet.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while submitting the DataSet.</param>
    /// <param name="name">The name of the DataSet that failed to submit.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71042,
        Level = LogLevel.Error,
        Message = "Failed to submit DataSet '{name}'")]
    public static partial IGenericMessage SubmitDataSetException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that the DataSet and DataStore types are being loaded for the wizard.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11043,
        Level = LogLevel.Trace,
        Message = "Loading DataSet and DataStore types for wizard")]
    public static partial IGenericMessage LoadingWizardTypes(ILogger logger);

    /// <summary>
    /// Logs that the DataSet and DataStore types were loaded for the wizard, with their respective counts.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetTypeCount">The number of DataSet types that were loaded.</param>
    /// <param name="dataStoreTypeCount">The number of DataStore types that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11044,
        Level = LogLevel.Information,
        Message = "Loaded {dataSetTypeCount} DataSet types and {dataStoreTypeCount} DataStore types for wizard")]
    public static partial IGenericMessage LoadedWizardTypes(ILogger logger, int dataSetTypeCount, int dataStoreTypeCount);

    /// <summary>
    /// Logs that loading the wizard types for the named category failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="category">The name of the category whose wizard types failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71043,
        Level = LogLevel.Warning,
        Message = "Failed to load wizard types for category '{category}'")]
    public static partial IGenericMessage LoadWizardTypesFailed(ILogger logger, string category);

    /// <summary>
    /// Logs that the capabilities for the named connection type are being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="connectionTypeName">The name of the connection type whose capabilities are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11045,
        Level = LogLevel.Trace,
        Message = "Loading capabilities for connection type '{connectionTypeName}'")]
    public static partial IGenericMessage LoadingCapabilities(ILogger logger, string connectionTypeName);

    /// <summary>
    /// Logs that the field types for the named connection type were loaded, with their count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="fieldTypeCount">The number of field types that were loaded.</param>
    /// <param name="connectionTypeName">The name of the connection type whose field types were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11046,
        Level = LogLevel.Debug,
        Message = "Loaded {fieldTypeCount} field types for connection type '{connectionTypeName}'")]
    public static partial IGenericMessage LoadedCapabilities(ILogger logger, int fieldTypeCount, string connectionTypeName);

    /// <summary>
    /// Logs that loading the capabilities for the named connection type failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="connectionTypeName">The name of the connection type whose capabilities failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71044,
        Level = LogLevel.Warning,
        Message = "Failed to load capabilities for connection type '{connectionTypeName}'")]
    public static partial IGenericMessage LoadCapabilitiesFailed(ILogger logger, string connectionTypeName);

    /// <summary>
    /// Logs that the DataSet wizard is being initialized from the named container in the named DataStore.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore containing the source container.</param>
    /// <param name="containerName">The name of the container the wizard is being initialized from.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11047,
        Level = LogLevel.Trace,
        Message = "Initializing DataSet wizard from container '{containerName}' in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage InitializingFromContainer(ILogger logger, string dataStoreName, string containerName);

    /// <summary>
    /// Logs that the DataSet wizard was initialized from the named container in the named DataStore, with the field count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="fieldCount">The number of fields the wizard was initialized with.</param>
    /// <param name="dataStoreName">The name of the DataStore containing the source container.</param>
    /// <param name="containerName">The name of the container the wizard was initialized from.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11048,
        Level = LogLevel.Debug,
        Message = "Initialized DataSet wizard from container '{containerName}' in DataStore '{dataStoreName}' with {fieldCount} fields")]
    public static partial IGenericMessage InitializedFromContainer(ILogger logger, int fieldCount, string dataStoreName, string containerName);

    /// <summary>
    /// Logs that the named container was not found in the named DataStore during wizard initialization.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore that was searched.</param>
    /// <param name="containerName">The name of the container that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Warning,
        Message = "Container '{containerName}' not found in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage InitializeFromContainerFailed(ILogger logger, string dataStoreName, string containerName);

    /// <summary>
    /// Logs that fields are being loaded for the named source.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sourceName">The name of the source whose fields are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11049,
        Level = LogLevel.Trace,
        Message = "Loading fields for source '{sourceName}'")]
    public static partial IGenericMessage LoadingFieldsForSource(ILogger logger, string sourceName);

    /// <summary>
    /// Logs that fields were loaded for the named source.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of fields that were loaded.</param>
    /// <param name="sourceName">The name of the source whose fields were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11050,
        Level = LogLevel.Debug,
        Message = "Loaded {count} fields for source '{sourceName}'")]
    public static partial IGenericMessage LoadedFieldsForSource(ILogger logger, int count, string sourceName);

    /// <summary>
    /// Logs that loading fields for the named source failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sourceName">The name of the source whose fields failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71045,
        Level = LogLevel.Warning,
        Message = "Failed to load fields for source '{sourceName}'")]
    public static partial IGenericMessage LoadFieldsForSourceFailed(ILogger logger, string sourceName);

    /// <summary>
    /// Logs that an exception was thrown while loading fields for the named source.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading fields.</param>
    /// <param name="sourceName">The name of the source whose fields failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71046,
        Level = LogLevel.Error,
        Message = "Failed to load fields for source '{sourceName}'")]
    public static partial IGenericMessage LoadFieldsForSourceException(ILogger logger, Exception exception, string sourceName);

    /// <summary>
    /// Logs that the join configuration was updated.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11051,
        Level = LogLevel.Trace,
        Message = "Updated join configuration")]
    public static partial IGenericMessage UpdateJoinConfiguration(ILogger logger);
}
