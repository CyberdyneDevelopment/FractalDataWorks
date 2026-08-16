using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging for <see cref="Fdw.UI.Components.Services.ConnectionIconMapper"/> operations.
/// EventId range: 11018-11020.
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class ConnectionIconMapperLog
{
    /// <summary>Logs entry to <c>FromType</c>.</summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Trace,
        Message = "Mapping connection type '{connectionType}' to icon metadata")]
    public static partial IGenericMessage MappingConnectionType(ILogger logger, string? connectionType);

    /// <summary>Logs the icon metadata resolved for a connection type.</summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Debug,
        Message = "Connection type '{connectionType}' mapped to icon '{iconKey}' in category '{iconCategory}'")]
    public static partial IGenericMessage MappedConnectionType(ILogger logger, string? connectionType, string iconKey, string iconCategory);

    /// <summary>Logs when a connection type name does not match any known category and falls back to a generic icon keyed off its own name.</summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Debug,
        Message = "Connection type '{connectionType}' did not match a known icon category; using a generic fallback icon")]
    public static partial IGenericMessage UnrecognizedConnectionType(ILogger logger, string connectionType);
}
