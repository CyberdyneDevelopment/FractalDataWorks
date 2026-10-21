using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for the category endpoints (FDW-1465): creating, changing and retiring a category, and reading the tree.
/// EventId range: 14200-14209.
/// </summary>
[MessageLoggingTypeCode("CATEGORIES")]
public static partial class CategoryEndpointLog
{
    /// <summary>Logs a category being created.</summary>
    [MessageLogging(EventId = 14200, Level = LogLevel.Information, Message = "Creating {type} category '{name}' under '{parent}'")]
    public static partial IGenericMessage Creating(ILogger logger, string type, string name, string? parent);

    /// <summary>Logs a category being changed: renamed, described or moved.</summary>
    [MessageLogging(EventId = 14201, Level = LogLevel.Information, Message = "Changing category '{name}': display name '{displayName}', under '{parent}'")]
    public static partial IGenericMessage Changing(ILogger logger, string name, string displayName, string? parent);

    /// <summary>Logs a category being retired.</summary>
    [MessageLogging(EventId = 14202, Level = LogLevel.Information, Message = "Retiring category '{name}'")]
    public static partial IGenericMessage Retiring(ILogger logger, string name);

    /// <summary>Logs the tree being read.</summary>
    [MessageLogging(EventId = 14203, Level = LogLevel.Debug, Message = "Read the category tree: {topLevel} top-level categor(ies)")]
    public static partial IGenericMessage TreeRead(ILogger logger, int topLevel);

    /// <summary>Logs a category that was saved and cannot be read back.</summary>
    [MessageLogging(EventId = 14204, Level = LogLevel.Error, Message = "Category '{name}' was saved and cannot be read back")]
    public static partial IGenericMessage SavedButUnreadable(ILogger logger, string name);
}
