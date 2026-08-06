using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Configuration.Endpoints.Logging;

/// <summary>
/// MessageLogging for configuration instance endpoint operations.
/// EventId range: 4140-4160
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS2")]
public static partial class ConfigurationEndpointLog
{
    /// <summary>Logs listing configuration instances with the specified filter.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "Listing configuration instances with filter '{filter}'")]
    public static partial IGenericMessage ListingInstances(ILogger logger, string filter);

    /// <summary>Logs the count of configuration instances retrieved.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Retrieved {count} configuration instances")]
    public static partial IGenericMessage InstancesRetrieved(ILogger logger, int count);

    /// <summary>Logs a table query failure during a configuration operation.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "Table query failed for '{tableName}' during '{operation}'")]
    public static partial IGenericMessage TableQueryFailed(ILogger logger, Exception ex, string tableName, string operation);

    /// <summary>Logs getting a configuration instance by name and category.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Getting configuration instance '{name}' in category '{category}'")]
    public static partial IGenericMessage GettingInstance(ILogger logger, string category, string name);

    /// <summary>Logs when no configuration types are found for a category.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "No configuration types found for category '{category}'")]
    public static partial IGenericMessage NoCategoryTypes(ILogger logger, string category);

    /// <summary>Logs when a configuration instance is not found.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "Configuration instance '{name}' not found in category '{category}'")]
    public static partial IGenericMessage InstanceNotFound(ILogger logger, string name, string category);

    /// <summary>Logs creating a configuration instance.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "Creating configuration instance '{name}' of type '{serviceType}' in category '{category}'")]
    public static partial IGenericMessage CreatingInstance(ILogger logger, string category, string serviceType, string name);

    /// <summary>Logs when a configuration type is not found for a category and service type.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning,
        Message = "Configuration type not found for category '{category}' service type '{serviceType}'")]
    public static partial IGenericMessage TypeNotFound(ILogger logger, string category, string serviceType);

    /// <summary>Logs when a configuration instance already exists.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "Configuration instance '{name}' already exists")]
    public static partial IGenericMessage InstanceAlreadyExists(ILogger logger, string name);

    /// <summary>Logs a failure to create a configuration instance.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to create configuration instance: {error}")]
    public static partial IGenericMessage CreateFailed(ILogger logger, string error);

    /// <summary>Logs successful creation of a configuration instance.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Configuration instance created successfully")]
    public static partial IGenericMessage InstanceCreated(ILogger logger);

    /// <summary>Logs updating a configuration instance.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Updating configuration instance '{name}' in category '{category}'")]
    public static partial IGenericMessage UpdatingInstance(ILogger logger, string category, string name);

    /// <summary>Logs a failure to update a configuration instance.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Failed to update configuration instance: {error}")]
    public static partial IGenericMessage UpdateFailed(ILogger logger, string error);

    /// <summary>Logs successful update of a configuration instance.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Configuration instance updated successfully")]
    public static partial IGenericMessage InstanceUpdated(ILogger logger);

    /// <summary>Logs deleting a configuration instance.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "Deleting configuration instance '{name}' in category '{category}'")]
    public static partial IGenericMessage DeletingInstance(ILogger logger, string category, string name);

    /// <summary>Logs a failure to delete a configuration instance.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "Failed to delete configuration instance: {error}")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, string error);

    /// <summary>Logs successful deletion of a configuration instance.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Configuration instance deleted successfully")]
    public static partial IGenericMessage InstanceDeleted(ILogger logger);

    /// <summary>Logs when a configuration instance is not found by name.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning,
        Message = "Configuration instance '{name}' not found")]
    public static partial IGenericMessage InstanceNotFoundByName(ILogger logger, string name);
}
