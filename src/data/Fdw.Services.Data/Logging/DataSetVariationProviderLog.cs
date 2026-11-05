using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Source-generated logging methods for <see cref="DataSetVariationConfigurationProvider"/> (FDW-3638).
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetVariationProviderLog
{
    /// <summary>Traces a data set that was read as a variation and is of another kind.</summary>
    [MessageLogging(EventId = 11720, Level = LogLevel.Debug,
        Message = "'{name}' is a data set of kind {kind}, not a variation, so the variation read answers nothing")]
    public static partial IGenericMessage NotAVariation(ILogger logger, string name, string kind);

    /// <summary>Traces a variation being saved through the data set provider.</summary>
    [MessageLogging(EventId = 11721, Level = LogLevel.Debug,
        Message = "Saving the variation '{name}' as a data set of kind Variation")]
    public static partial IGenericMessage Saving(ILogger logger, string name);

    /// <summary>Traces a variation being retired through the data set provider.</summary>
    [MessageLogging(EventId = 11722, Level = LogLevel.Debug,
        Message = "Retiring the variation {id} through the data set provider")]
    public static partial IGenericMessage Retiring(ILogger logger, Guid id);

    /// <summary>Refuses to retire, as a variation, a data set that is of another kind or does not exist.</summary>
    [MessageLogging(EventId = 41103, Level = LogLevel.Warning,
        Message = "{id} is not a variation, so the variation provider will not retire it")]
    public static partial IGenericMessage DeleteRefused(ILogger logger, Guid id);
}
