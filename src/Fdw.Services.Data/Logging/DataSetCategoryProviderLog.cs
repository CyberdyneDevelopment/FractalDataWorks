using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="DataSetCategoryProvider"/> operations.
/// EventId range: 5405-5419
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetCategoryProviderLog
{
    /// <summary>Logged at Trace on entry to the Initialize phase.</summary>
    [MessageLogging(EventId = 11046, Level = LogLevel.Trace, Message = "DataSetCategoryProvider.Initialize starting")]
    public static partial IGenericMessage InitializeStarted(ILogger logger);

    /// <summary>Logged at Information after categories are loaded from the database.</summary>
    [MessageLogging(EventId = 11047, Level = LogLevel.Information, Message = "DataSetCategoryProvider.Initialize: loaded {count} categories from data.DataSetCategory")]
    public static partial IGenericMessage Initialized(ILogger logger, int count);

    /// <summary>Logged at Trace for each category registered into DataSetCategories.</summary>
    [MessageLogging(EventId = 11048, Level = LogLevel.Trace, Message = "DataSetCategoryProvider: registered category '{name}'")]
    public static partial IGenericMessage CategoryRegistered(ILogger logger, string name);

    /// <summary>Logged at Warning when the database query fails; compile-time categories remain available.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Warning, Message = "DataSetCategoryProvider.Initialize: failed to load categories from database — runtime categories unavailable: {reason}")]
    public static partial IGenericMessage LoadFailed(ILogger logger, string reason);

    /// <summary>Logged at Error when no configuration gateway serves the connection categories live on.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection no gateway serves.</param>
    [MessageLogging(EventId = 61022, Level = LogLevel.Error, Message = "DataSetCategoryProvider.Initialize: no configuration gateway is registered for connection '{connectionName}' — runtime categories unavailable")]
    public static partial IGenericMessage GatewayUnavailable(ILogger logger, string connectionName);

    /// <summary>Logged at Warning when a category row has a blank Name and is skipped.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "DataSetCategoryProvider.Initialize: skipping category row Id='{id}' — Name is blank")]
    public static partial IGenericMessage SkippingBlankName(ILogger logger, System.Guid id);

    /// <summary>Logged at Trace when a category already exists in DataSetCategories (duplicate guard).</summary>
    [MessageLogging(EventId = 11049, Level = LogLevel.Trace, Message = "DataSetCategoryProvider.Initialize: category '{name}' already registered (compile-time), skipping")]
    public static partial IGenericMessage AlreadyRegistered(ILogger logger, string name);

    /// <summary>Logged at Debug when the category load is cancelled by host shutdown (a clean, expected exit).</summary>
    [MessageLogging(EventId = 11050, Level = LogLevel.Debug, Message = "DataSetCategoryProvider.Initialize: category load cancelled during host shutdown")]
    public static partial IGenericMessage LoadCancelled(ILogger logger, System.Exception ex);
}
