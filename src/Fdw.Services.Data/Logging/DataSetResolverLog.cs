using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Source-generated logging methods for <see cref="DataSetResolver"/> (FDW-2390).
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetResolverLog
{
    /// <summary>Traces what a name resolved to.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Resolved '{name}' to a {kind}")]
    public static partial IGenericMessage Resolved(ILogger logger, string name, string kind);

    /// <summary>Refuses a name that a data set and a variation both have.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "'{name}' is the name of both a data set and a {kind} variation, and one name must mean one thing")]
    public static partial IGenericMessage Ambiguous(ILogger logger, string name, string kind);

    /// <summary>Refuses a variation whose data set no longer exists.</summary>
    [MessageLogging(EventId = 40000, Level = LogLevel.Error,
        Message = "The {kind} variation '{name}' varies the data set {dataSetId}, which no longer exists")]
    public static partial IGenericMessage RootMissing(ILogger logger, string name, string kind, string dataSetId);

    /// <summary>Refuses a chain of views longer than the limit.</summary>
    [MessageLogging(EventId = 40002, Level = LogLevel.Error,
        Message = "The {kind} variation '{name}' has more than {limit} views in its chain, and a cut chain would drop a filter")]
    public static partial IGenericMessage ChainTooDeep(ILogger logger, string name, string kind, int limit);

    /// <summary>Refuses a variation whose parent view no longer exists.</summary>
    [MessageLogging(EventId = 40001, Level = LogLevel.Error,
        Message = "The {kind} variation '{name}' narrows the view {parentId}, which no longer exists")]
    public static partial IGenericMessage ParentMissing(ILogger logger, string name, string kind, string parentId);
}
