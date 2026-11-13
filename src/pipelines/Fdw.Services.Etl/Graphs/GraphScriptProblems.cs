using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The semantic check of a graph's SQL steps (FDW-5457): a save refuses what the check can prove wrong, and warns of what it cannot judge.
/// </summary>
/// <remarks>
/// <para>
/// Refused: a script that does not parse in its data store's language (DATA-21440), a table the store's catalogue does not hold (DATA-21441), a table
/// named with no schema that more than one schema holds (DATA-21442), and a data store that cannot be read (DATA-21443).
/// </para>
/// <para>
/// Warned, and saved as before: a data store whose kind has no script reader on this host (DATA-21444), names in another database or server, which are
/// legal references the catalogue cannot hold (DATA-21445), and a data store with no catalogued containers, whose tables cannot be checked until it is
/// discovered (DATA-21449). A name with a part given at run time, and every name of a script whose data store is a parameter, are judged when the step
/// runs, and only logged. A table the script creates itself need not exist yet. A procedure the script runs and a table-valued function it reads are
/// neither checked nor drawn.
/// </para>
/// <para>
/// Why a check apart from the shape check: what a script names is judged against the data store's catalogue, so the routes that save and validate a
/// graph read the store and pass the reader in, as <see cref="DestinationGuidColumns"/> does for data set fields. Each finding is its own coded
/// message, placed on the step's script setting.
/// </para>
/// </remarks>
public static class GraphScriptProblems
{
    private const string ScriptSetting = "script";

    /// <summary>Judges every SQL step of a graph.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="dataStore">Reads a data store by name, with its paths and containers.</param>
    /// <param name="logger">The logger the refusals and warnings are written to.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>The problems a save is refused for and the warnings, each placed on the script setting of its step.</returns>
    public static async Task<GraphFindings> Judge(
        GraphPipelineConfiguration graph,
        Func<string, CancellationToken, Task<IGenericResult<IDataStoreImplementationConfiguration>>> dataStore,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var problems = new List<GraphProblem>();
        var warnings = new List<GraphProblem>();
        foreach (var script in GraphScripts.Held(graph))
        {
            await JudgeStep(script, dataStore, new GraphFindingLists(problems, warnings), logger, cancellationToken).ConfigureAwait(false);
        }

        return new GraphFindings(problems, warnings);
    }

    private static async Task JudgeStep(
        GraphScript script,
        Func<string, CancellationToken, Task<IGenericResult<IDataStoreImplementationConfiguration>>> dataStore,
        GraphFindingLists findings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (VariableText.Names(script.DataStore).Count > 0)
        {
            GraphScriptLog.ScriptStoreAtRunTime(logger, script.Component, script.DataStore);
            return;
        }

        var store = await dataStore(script.DataStore, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is null)
        {
            findings.Problems.Add(Finding(script, GraphScriptLog.ScriptStoreNotRead(logger, script.Component, script.DataStore, store.NoValueReason().Message)));
            return;
        }

        if (ReferenceEquals(ScriptReaders.ByName(store.Value.Implementation), ScriptReaders.NotFound))
        {
            findings.Warnings.Add(Finding(script, GraphScriptLog.ScriptLanguageNotRead(logger, script.Component, script.DataStore, store.Value.Implementation)));
            return;
        }

        var reading = GraphScriptReading.Read(script, store.Value, logger);
        if (reading.IsFailure || reading.Value is null)
        {
            findings.Problems.Add(Finding(script, reading.NoValueReason()));
            return;
        }

        Names(script, store.Value, reading.Value, findings, logger);
    }

    private static void Names(GraphScript script, IDataStoreImplementationConfiguration store, ScriptReading reading, GraphFindingLists findings, ILogger logger)
    {
        if (reading.OutsideStore.Count > 0)
        {
            findings.Warnings.Add(Finding(script, GraphScriptLog.ScriptNamesOutsideStore(logger, script.Component, Quoted(reading.OutsideStore), script.DataStore)));
        }

        if (reading.RunTimeNamed.Count > 0)
        {
            GraphScriptLog.ScriptNamesAtRunTime(logger, script.Component, script.DataStore, Quoted(reading.RunTimeNamed));
        }

        var named = reading.Reads.Concat(reading.Writes).Where(name => !Created(reading, name)).DistinctBy(name => name.Display, StringComparer.OrdinalIgnoreCase).ToList();
        if (named.Count == 0)
        {
            return;
        }

        if (!store.Paths.Any(path => path.Containers.Count > 0))
        {
            findings.Warnings.Add(Finding(script, GraphScriptLog.ScriptStoreNotCatalogued(logger, script.DataStore, script.Component)));
            return;
        }

        foreach (var name in named)
        {
            Catalogued(script, store, name, findings, logger);
        }
    }

    private static void Catalogued(GraphScript script, IDataStoreImplementationConfiguration store, ScriptObjectName name, GraphFindingLists findings, ILogger logger)
    {
        var paths = ScriptObjectContainers.PathsHolding(store, name);
        if (paths.Count == 0)
        {
            findings.Problems.Add(Finding(script, GraphScriptLog.ScriptObjectMissing(logger, script.Component, name.Display, script.DataStore)));
        }
        else if (paths.Count > 1)
        {
            findings.Problems.Add(Finding(script, GraphScriptLog.ScriptObjectAmbiguous(logger, script.Component, name.Name, script.DataStore, Quoted(paths))));
        }
    }

    // A table the script creates is judged by the script itself: a name with no path matches one created in any path, and the other way.
    private static bool Created(ScriptReading reading, ScriptObjectName name)
        => reading.Creates.Any(created => string.Equals(created.Name, name.Name, StringComparison.OrdinalIgnoreCase)
            && (created.Path is null || name.Path is null || string.Equals(created.Path, name.Path, StringComparison.OrdinalIgnoreCase)));

    private static string Quoted(IEnumerable<string> names) => string.Join(", ", names.Select(name => $"'{name}'"));

    private static GraphProblem Finding(GraphScript script, IGenericMessage message)
        => new(["components", script.Component, "settings", ScriptSetting], message.Message);
}
