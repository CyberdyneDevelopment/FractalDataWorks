using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for DataStoreConfigurationProvider operations.
/// EventIds are allocated sequentially against the shared "DATA" typecode series used across
/// src/Fdw.Services.Data (see EVENTID-ALLOCATION.md), not a private per-file range: 11109-11118,
/// 11265-11266 (flow), 31022-31023/41008/61011-61012/91014-91015/91057 (warning/error).
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataStoreConfigurationProviderLog
{
    /// <summary>
    /// Logs that a DataStore was resolved via the DataStoreConfigurationProvider.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore that was resolved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11109, Level = LogLevel.Debug,
        Message = "DataStore '{dataStoreName}' resolved via DataStoreConfigurationProvider")]
    public static partial IGenericMessage Resolved(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs that a DataStore lookup by name is querying the provider.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataStore being queried.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11110, Level = LogLevel.Trace,
        Message = "DataStore Get(name='{name}') querying provider")]
    public static partial IGenericMessage GetByNameQuerying(ILogger logger, string name);

    /// <summary>
    /// Logs that a DataStore lookup by id is querying the provider.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the DataStore being queried.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11111, Level = LogLevel.Trace,
        Message = "DataStore Get(id='{id}') querying provider")]
    public static partial IGenericMessage GetByIdQuerying(ILogger logger, string id);

    /// <summary>
    /// Logs that a DataStore get-all operation is querying the provider and merging with system configs.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11112, Level = LogLevel.Debug,
        Message = "DataStore GetAll querying provider, merging with system configs")]
    public static partial IGenericMessage GetAllQuerying(ILogger logger);

    /// <summary>
    /// Logs that DataStore system and user configs were merged, reporting both counts.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="systemCount">The number of system configs merged.</param>
    /// <param name="userCount">The number of user configs merged.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11113, Level = LogLevel.Debug,
        Message = "DataStore merged {systemCount} system + {userCount} user configs")]
    public static partial IGenericMessage MergedCounts(ILogger logger, int systemCount, int userCount);

    /// <summary>
    /// Logs when child assembly (Paths/Containers/Fields) fails for a DataStore.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore whose child assembly failed.</param>
    /// <param name="error">The error describing why the child assembly failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91014, Level = LogLevel.Warning,
        Message = "DataStore '{dataStoreName}' child assembly failed: {error}")]
    public static partial IGenericMessage ChildAssemblyFailed(ILogger logger, string dataStoreName, string error);

    /// <summary>
    /// Logs that a typed DataStore provider was registered for a given service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The service option type the typed DataStore provider was registered for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11114, Level = LogLevel.Trace,
        Message = "Registering typed DataStore provider for service option type '{serviceOptionType}'")]
    public static partial IGenericMessage TypedProviderRegistered(ILogger logger, string serviceOptionType);

    /// <summary>
    /// Logs that the typed DataStore body is being loaded using a service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataStore whose typed body is being loaded.</param>
    /// <param name="serviceOptionType">The service option type used to load the typed DataStore body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11115, Level = LogLevel.Trace,
        Message = "Loading typed DataStore body for '{name}' using service option type '{serviceOptionType}'")]
    public static partial IGenericMessage LoadingTypedBody(ILogger logger, string name, string serviceOptionType);

    /// <summary>
    /// Logs that no typed DataStore provider is registered for the requested service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The service option type for which no typed DataStore provider was found.</param>
    /// <param name="name">The name of the DataStore that could not be loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61011, Level = LogLevel.Error,
        Message = "No typed DataStore provider registered for service option type '{serviceOptionType}' (DataStore '{name}')")]
    public static partial IGenericMessage NoTypedProviderForServiceOptionType(ILogger logger, string serviceOptionType, string name);

    /// <summary>
    /// Logs that loading the typed DataStore body failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the typed body load to fail.</param>
    /// <param name="name">The name of the DataStore whose typed body failed to load.</param>
    /// <param name="serviceOptionType">The service option type used when the load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91015, Level = LogLevel.Error,
        Message = "Failed to load typed DataStore body for '{name}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoadFailed(ILogger logger, Exception exception, string name, string serviceOptionType);

    /// <summary>
    /// Logs that a DataStore has no ServiceOptionType, so its typed body cannot be loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataStore missing a ServiceOptionType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61012, Level = LogLevel.Error,
        Message = "DataStore '{name}' has no ServiceOptionType — typed body cannot be loaded")]
    public static partial IGenericMessage MissingServiceOptionType(ILogger logger, string name);

    /// <summary>
    /// Logs that the typed DataStore body was successfully loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataStore whose typed body was loaded.</param>
    /// <param name="serviceOptionType">The service option type used to load the typed DataStore body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11116, Level = LogLevel.Trace,
        Message = "Typed DataStore body loaded for '{name}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoaded(ILogger logger, string name, string serviceOptionType);

    /// <summary>
    /// Logs that a DataStore uses its header as the typed body, with no separate typed-body table.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataStore that uses the header as the typed body.</param>
    /// <param name="serviceOptionType">The service option type of the DataStore.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11117, Level = LogLevel.Trace,
        Message = "DataStore '{name}' (ServiceOptionType '{serviceOptionType}') uses the header as the typed body — no separate typed-body table")]
    public static partial IGenericMessage HeaderIsTypedBody(ILogger logger, string name, string serviceOptionType);

    /// <summary>
    /// Logs that a DataStore was not found, so a container cannot be added.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The name of the DataStore that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31022, Level = LogLevel.Error,
        Message = "DataStore '{storeName}' not found — cannot add container")]
    public static partial IGenericMessage StoreNotFoundForAddContainer(ILogger logger, string storeName);

    /// <summary>
    /// Logs that a path was not found in a DataStore, so a container cannot be added.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="pathName">The name of the path that was not found.</param>
    /// <param name="storeName">The name of the DataStore the path was expected in.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31023, Level = LogLevel.Error,
        Message = "Path '{pathName}' not found in DataStore '{storeName}' — cannot add container")]
    public static partial IGenericMessage PathNotFoundForAddContainer(ILogger logger, string pathName, string storeName);

    /// <summary>
    /// Logs that a container already exists in a path of a DataStore.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the container that already exists.</param>
    /// <param name="pathName">The name of the path the container exists in.</param>
    /// <param name="storeName">The name of the DataStore the path belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41008, Level = LogLevel.Error,
        Message = "Container '{containerName}' already exists in path '{pathName}' of DataStore '{storeName}'")]
    public static partial IGenericMessage ContainerAlreadyExists(ILogger logger, string containerName, string pathName, string storeName);

    /// <summary>
    /// Logs that a container was added to a path in a DataStore.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the container that was added.</param>
    /// <param name="pathName">The name of the path the container was added to.</param>
    /// <param name="storeName">The name of the DataStore the path belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11118, Level = LogLevel.Information,
        Message = "Container '{containerName}' added to path '{pathName}' in DataStore '{storeName}'")]
    public static partial IGenericMessage ContainerAdded(ILogger logger, string containerName, string pathName, string storeName);

    /// <summary>
    /// Logs that the all-items DataStore list is being composed to full aggregates (Paths/Containers/Fields)
    /// instead of returning bare header rows.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why (FDW-558): next available EventId in this assembly's DATA-typecode flow series — grepped
    // src/Fdw.Services.Data for the highest existing "EventId = 111xx" before reserving (11264, StatSetServiceLog).
    [MessageLogging(EventId = 11265, Level = LogLevel.Trace,
        Message = "Composing DataStore list to full aggregates (Paths/Containers/Fields)")]
    public static partial IGenericMessage ComposingDataStoreList(ILogger logger);

    /// <summary>
    /// Logs that the all-items DataStore list finished composing, reporting the composed count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of DataStores composed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11266, Level = LogLevel.Information,
        Message = "DataStore list composed: {count} stores with full Paths/Containers/Fields")]
    public static partial IGenericMessage DataStoreListComposed(ILogger logger, int count);

    /// <summary>
    /// Logs that composing one DataStore's aggregate (during the all-items list read) failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore whose aggregate compose failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why (FDW-558): 91025 is the next number after this assembly's own DATA-typecode error series
    // (91000-91024) but COLLIDES with Fdw.Data.Components/Logging/DataStoreEditorProviderLog (a
    // different assembly/typecode "DATACOMPONENTS" that independently claimed 91025-91056) — grepping
    // broadly (not just src/Fdw.Services.Data) before reserving found that block fully packed, so this
    // uses the next number free ACROSS THE WHOLE src TREE (91057) rather than adding another documented
    // "acceptable" cross-assembly collision.
    [MessageLogging(EventId = 91057, Level = LogLevel.Error,
        Message = "Failed to compose DataStore '{dataStoreName}' aggregate during list read — no fallback, failing the whole list")]
    public static partial IGenericMessage DataStoreListComposeFailed(ILogger logger, string dataStoreName);
}
