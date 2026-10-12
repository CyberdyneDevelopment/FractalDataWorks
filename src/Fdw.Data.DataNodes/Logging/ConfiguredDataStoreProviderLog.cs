using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Source-generated logging methods for <see cref="Fdw.Services.Data.ConfiguredDataStoreProvider"/>.
/// </summary>
/// <remarks>
/// Why: <see cref="Fdw.Services.Data.ConfiguredDataStoreProvider"/> is the pure, gateway-free provider
/// that lives in <c>Fdw.Data.DataNodes</c> alongside <c>DataStoreLoaderLog</c> — it cannot reuse
/// <c>DataStoreProviderLog</c> (that class stays in <c>Fdw.Services.Data</c>, the server-side package).
/// EventIds allocated fresh, clear of every EventId already used under the "DATA" TypeCode across both
/// packages (verified 2026-07-11: DATA cat1 max 11266, cat2 max 21009, cat3 max 31041, cat7 max 71049).
/// </remarks>
[MessageLoggingTypeCode("DATA")]
public static partial class ConfiguredDataStoreProviderLog
{
    // ============================================================
    // Trace entry points (cat 1: Info/Debug/Trace) — 11300-11304
    // ============================================================

    /// <summary>Traces entry into <c>ConfiguredDataStoreProvider.Get(string)</c>.</summary>
    [MessageLogging(EventId = 11300, Level = LogLevel.Trace,
        Message = "[ConfiguredDataStoreProvider] Entering Get for DataStore '{name}'")]
    public static partial IGenericMessage TraceGetByNameEntry(ILogger logger, string name);

    /// <summary>Traces entry into <c>ConfiguredDataStoreProvider.Get(Guid)</c>.</summary>
    [MessageLogging(EventId = 11301, Level = LogLevel.Trace,
        Message = "[ConfiguredDataStoreProvider] Entering Get for DataStore Id '{id}'")]
    public static partial IGenericMessage TraceGetByIdEntry(ILogger logger, Guid id);

    /// <summary>Traces entry into <c>ConfiguredDataStoreProvider.Get()</c> (all DataStores).</summary>
    [MessageLogging(EventId = 11302, Level = LogLevel.Trace,
        Message = "[ConfiguredDataStoreProvider] Entering Get (all DataStores)")]
    public static partial IGenericMessage TraceGetAllEntry(ILogger logger);

    /// <summary>Traces entry into the DataPath dot-walk overload.</summary>
    [MessageLogging(EventId = 11303, Level = LogLevel.Trace,
        Message = "[ConfiguredDataStoreProvider] Entering Get for DataStore '{dataStoreName}', Path '{pathName}'")]
    public static partial IGenericMessage TraceGetPathEntry(ILogger logger, string dataStoreName, string pathName);

    /// <summary>Traces entry into the DataContainer dot-walk overload.</summary>
    [MessageLogging(EventId = 11304, Level = LogLevel.Trace,
        Message = "[ConfiguredDataStoreProvider] Entering Get for DataStore '{dataStoreName}', Path '{pathName}', Container '{containerName}'")]
    public static partial IGenericMessage TraceGetContainerEntry(ILogger logger, string dataStoreName, string pathName, string containerName);

    // ============================================================
    // Success (cat 1) — 11305-11306
    // ============================================================

    /// <summary>Logs when a DataStore is built from its resolved configuration.</summary>
    [MessageLogging(EventId = 11305, Level = LogLevel.Information,
        Message = "[ConfiguredDataStoreProvider] Built DataStore '{dataStoreName}' from configuration using store type '{storeType}'")]
    public static partial IGenericMessage StoreBuilt(ILogger logger, string dataStoreName, string storeType);

    /// <summary>Logs when all DataStores are retrieved.</summary>
    [MessageLogging(EventId = 11306, Level = LogLevel.Information,
        Message = "[ConfiguredDataStoreProvider] Retrieved {count} DataStores")]
    public static partial IGenericMessage AllStoresRetrieved(ILogger logger, int count);

    // ============================================================
    // Validation (cat 2) — 21050
    // ============================================================

    /// <summary>Returns a message indicating the DataStore name is required.</summary>
    [MessageLogging(EventId = 21050, Level = LogLevel.Error,
        Message = "[ConfiguredDataStoreProvider] DataStore name is required")]
    public static partial IGenericMessage StoreNameRequired(ILogger logger);

    // ============================================================
    // Missing / NotFound (cat 3) — 31100-31101
    // ============================================================

    /// <summary>Logs when a DataStore configuration is not found by name.</summary>
    [MessageLogging(EventId = 31100, Level = LogLevel.Warning,
        Message = "[ConfiguredDataStoreProvider] DataStore '{name}' not found")]
    public static partial IGenericMessage StoreNotFound(ILogger logger, string name);

    /// <summary>Logs when a DataStore configuration is not found by Id.</summary>
    [MessageLogging(EventId = 31101, Level = LogLevel.Warning,
        Message = "[ConfiguredDataStoreProvider] DataStore with Id '{id}' not found")]
    public static partial IGenericMessage StoreByIdNotFound(ILogger logger, Guid id);

    /// <summary>Logs when a DataStore is omitted from the Get() aggregate because its build failed.</summary>
    [MessageLogging(EventId = 31102, Level = LogLevel.Warning,
        Message = "[ConfiguredDataStoreProvider] DataStore '{name}' failed to build and was omitted from the retrieved list")]
    public static partial IGenericMessage StoreSkippedInLoad(ILogger logger, string name);

    // ============================================================
    // Dependency (cat 7) — 71100
    // ============================================================

    /// <summary>Logs when the configuration provider fails to return all DataStore configurations.</summary>
    [MessageLogging(EventId = 71100, Level = LogLevel.Error,
        Message = "[ConfiguredDataStoreProvider] Failed to load all DataStore configurations from the configuration provider")]
    public static partial IGenericMessage LoadAllFailed(ILogger logger);
}
