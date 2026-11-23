using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// MessageLogging for the planner that pushes a graph's same-store joins into their store (FDW-6969): which parts of a graph run as one query in a
/// store, with the query, and why each other join, union or lookup runs in memory. None is a failure: the graph runs either way.
/// EventId range: 10100-10119 (category 1, non-error outcomes).
/// </summary>
[MessageLoggingTypeCode("PUSHDOWN")]
public static partial class PushdownLog
{
    /// <summary>Logs a graph read through a gateway that does not describe stores for a planner.</summary>
    [MessageLogging(EventId = 10100, Level = LogLevel.Trace,
        Message = "Graph '{graph}' runs in memory whole: the data gateway it reads through does not describe stores for pushing reads into them")]
    public static partial IGenericMessage PlanningNotOffered(ILogger logger, string graph);

    /// <summary>Logs a sub-graph that runs as one query in its store.</summary>
    [MessageLogging(EventId = 10101, Level = LogLevel.Debug,
        Message = "Graph '{graph}': '{component}' and what it reads ({members}) run as one query in data store '{dataStore}': {query}")]
    public static partial IGenericMessage SubGraphPushed(ILogger logger, string graph, string component, string members, string dataStore, string query);

    /// <summary>Logs a join, union or lookup that runs in memory, and why.</summary>
    [MessageLogging(EventId = 10102, Level = LogLevel.Trace,
        Message = "Graph '{graph}': '{component}' runs in memory: {reason}")]
    public static partial IGenericMessage LeftInMemory(ILogger logger, string graph, string component, string reason);

    /// <summary>Logs a test run, which caps each source's rows and so runs in memory whole.</summary>
    [MessageLogging(EventId = 10103, Level = LogLevel.Trace,
        Message = "Graph '{graph}' runs in memory whole: a test run caps the rows each source gives, which a pushed query would not")]
    public static partial IGenericMessage TestRunInMemory(ILogger logger, string graph);

    /// <summary>Logs the reads that feed a join, lookup or union across data stores (FDW-6970).</summary>
    [MessageLogging(EventId = 10104, Level = LogLevel.Debug,
        Message = "Graph '{graph}': '{component}' joins reads of more than one data store ({reads}); they run under the federation strategy")]
    public static partial IGenericMessage FederatedReadsFound(ILogger logger, string graph, string component, string reads);

    /// <summary>Logs a read whose data store cannot be judged, so it is not counted among a combiner's stores (FDW-6970).</summary>
    [MessageLogging(EventId = 10105, Level = LogLevel.Trace,
        Message = "Graph '{graph}': the data store '{read}' reads cannot be judged, so it is not counted among a combiner's stores: {reason}")]
    public static partial IGenericMessage FederatedReadStoreUnknown(ILogger logger, string graph, string read, string reason);

    /// <summary>Logs the federation strategy and options a graph's federated reads run under, and where each value came from (FDW-6970).</summary>
    [MessageLogging(EventId = 10106, Level = LogLevel.Debug,
        Message = "Graph '{graph}': {reads} read(s) run under federation strategy '{strategy}': {options}")]
    public static partial IGenericMessage FederatedReadsGated(ILogger logger, string graph, int reads, string strategy, string options);
}
