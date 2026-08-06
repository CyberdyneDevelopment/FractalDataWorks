using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging for <c>ObjectPicker&lt;TItem&gt;</c> operations.
/// EventId range: 4900-4900
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class ObjectPickerLog
{
    /// <summary>
    /// Logged when the async <c>ItemsSource</c> loader fails to produce the item list.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "ObjectPicker: failed to load items for type '{itemTypeName}'")]
    public static partial IGenericMessage LoadFailed(ILogger logger, Exception exception, string itemTypeName);
}
