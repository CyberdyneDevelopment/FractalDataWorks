using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging for <c>OptionPicker&lt;TTypeOption&gt;</c> operations.
/// EventId range: 4157-4159
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class OptionPickerLog
{
    /// <summary>
    /// Logged when the async <c>Source</c> factory fails to produce the option list.
    /// </summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "OptionPicker: failed to load options for type '{typeOptionName}'")]
    public static partial IGenericMessage LoadFailed(ILogger logger, Exception exception, string typeOptionName);
}
