using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for the unified calculation catalog (<see cref="CalculationCatalogProvider"/>).
/// </summary>
[MessageLoggingTypeCode("CALCULATIONS")]
public static partial class CalculationCatalogLog
{
    /// <summary>Logs that the unified calculation catalog was requested.</summary>
    [MessageLogging(EventId = 11061, Level = LogLevel.Trace, Message = "Requesting the unified calculation catalog")]
    public static partial IGenericMessage CatalogRequested(ILogger logger);

    /// <summary>Logs the count of items one source contributed to the catalog.</summary>
    [MessageLogging(EventId = 11062, Level = LogLevel.Trace, Message = "Source '{source}' listed {count} calculation catalog items")]
    public static partial IGenericMessage SourceCatalogListed(ILogger logger, string source, int count);

    /// <summary>Logs that the full catalog union was assembled.</summary>
    [MessageLogging(EventId = 11063, Level = LogLevel.Information, Message = "Assembled calculation catalog with {count} items across {sourceCount} sources")]
    public static partial IGenericMessage CatalogAssembled(ILogger logger, int count, int sourceCount);

    /// <summary>Logs that a requested catalog item was not found in any source.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Calculation catalog item '{source}/{name}' was not found")]
    public static partial IGenericMessage CalculationCatalogItemNotFound(ILogger logger, string source, string name);

    /// <summary>Logs that a calculation source failed to list its catalog items.</summary>
    [MessageLogging(EventId = 71015, Level = LogLevel.Error, Message = "Calculation source '{source}' failed to list its catalog items")]
    public static partial IGenericMessage SourceListFailed(ILogger logger, string source);

    /// <summary>Logs that an unexpected exception was thrown while assembling the catalog.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error, Message = "Failed to assemble the calculation catalog")]
    public static partial IGenericMessage CatalogAssemblyFailed(ILogger logger, Exception ex);
}
