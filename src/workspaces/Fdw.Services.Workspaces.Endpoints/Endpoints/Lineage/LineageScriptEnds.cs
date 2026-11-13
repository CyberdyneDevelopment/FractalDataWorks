using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The containers a SQL step's script reads and writes (FDW-5457), from a parse of the script in its data store's language: the tables it selects
/// from are reads, the tables it inserts into, updates, deletes from, merges into, truncates or selects into are writes.
/// </summary>
/// <remarks>
/// A stored script is read as it is: one that does not parse, or whose data store's kind has no reader on this host, draws no line and is logged,
/// and the rest of the pipeline is drawn as before. The reading's own refusal is not logged here, because it is a refusal only when a graph is
/// saved; here it is the reason the step draws nothing.
/// </remarks>
internal static class LineageScriptEnds
{
    /// <summary>Lists the containers a SQL step's script reads and writes.</summary>
    /// <param name="pipelineId">The pipeline the step is in.</param>
    /// <param name="script">The SQL step.</param>
    /// <param name="store">The data store it runs on, read with its paths and containers.</param>
    /// <param name="logger">The logger a script that draws nothing is written to.</param>
    /// <returns>Each container once for each way it is used; none when the script cannot be read.</returns>
    public static List<(PipelineContainerRef Container, bool Reads)> Of(
        Guid pipelineId, GraphScript script, IDataStoreImplementationConfiguration store, ILogger logger)
    {
        var reading = GraphScriptReading.Read(script, store, NullLogger.Instance);
        if (reading.IsFailure || reading.Value is null)
        {
            GraphScriptLog.ScriptNotDrawn(logger, pipelineId.ToString("D", CultureInfo.InvariantCulture), script.Component, reading.NoValueReason().Message);
            return [];
        }

        return
        [
            .. reading.Value.Reads.Select(name => (new PipelineContainerRef(script.DataStore, name.Path, name.Name), true)),
            .. reading.Value.Writes.Select(name => (new PipelineContainerRef(script.DataStore, name.Path, name.Name), false)),
        ];
    }
}
