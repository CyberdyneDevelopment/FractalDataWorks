using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging for <c>EntityPicker&lt;TItem&gt;</c> operations.
/// EventId range: 71015-71017
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class EntityPickerLog
{
    /// <summary>
    /// Logged when the async <c>SearchSource</c> delegate fails to load items.
    /// </summary>
    [MessageLogging(
        EventId = 71015,
        Level = LogLevel.Error,
        Message = "EntityPicker: failed to load items for type '{itemTypeName}' (search='{searchTerm}')")]
    public static partial IGenericMessage LoadFailed(ILogger logger, Exception exception, string itemTypeName, string searchTerm);

    /// <summary>
    /// Logged when the <c>SearchSource</c> delegate is null at load time.
    /// </summary>
    [MessageLogging(
        EventId = 71016,
        Level = LogLevel.Error,
        Message = "EntityPicker: SearchSource is required but was not provided (type '{itemTypeName}')")]
    public static partial IGenericMessage SearchSourceMissing(ILogger logger, string itemTypeName);
}
