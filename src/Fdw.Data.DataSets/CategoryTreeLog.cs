using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets;

/// <summary>MessageLogging for reading the category tree and for the category a request names or a thing is labelled with (FDW-1510).</summary>
[MessageLoggingTypeCode("DATASETS")]
public static partial class CategoryTreeLog
{
    /// <summary>Traces a category tree read that built a sound tree.</summary>
    [MessageLogging(EventId = 14210, Level = LogLevel.Trace,
        Message = "[CategoryTree] Read the {scope} category tree: {nodes} node(s), {topLevel} top-level")]
    public static partial IGenericMessage TreeRead(ILogger logger, string scope, int nodes, int topLevel);

    /// <summary>Traces the category a request names being looked up in a tree.</summary>
    [MessageLogging(EventId = 14211, Level = LogLevel.Trace,
        Message = "[CategoryTree] Looking up category '{name}' of the {type} type")]
    public static partial IGenericMessage Resolving(ILogger logger, string type, string name);

    /// <summary>Traces the category a request named being found.</summary>
    [MessageLogging(EventId = 14212, Level = LogLevel.Trace,
        Message = "[CategoryTree] Category '{name}' of the {type} type is {categoryId}")]
    public static partial IGenericMessage Resolved(ILogger logger, string type, string name, Guid categoryId);

    /// <summary>Traces a request that names no category.</summary>
    [MessageLogging(EventId = 14213, Level = LogLevel.Trace,
        Message = "[CategoryTree] The request names no {type} category")]
    public static partial IGenericMessage NoneNamed(ILogger logger, string type);

    /// <summary>Traces a category being labelled with its name and path.</summary>
    [MessageLogging(EventId = 14214, Level = LogLevel.Trace,
        Message = "[CategoryTree] Labelled category {categoryId} of the {type} type: '{path}'")]
    public static partial IGenericMessage Labelled(ILogger logger, string type, Guid categoryId, string path);
}
