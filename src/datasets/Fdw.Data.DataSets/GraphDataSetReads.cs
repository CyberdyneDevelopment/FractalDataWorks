using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// Reads what a Graph data set reads from what it stores (FDW-5296): the data sets, data stores and containers its components and stage tools name, and the
/// sources it only intends to read (story 9).
/// </summary>
/// <remarks>
/// <para>
/// This is the one reading lineage and status take of a Graph. A Graph has no source list of its own (<see cref="IDataSetImplementationConfiguration.Sources"/> is empty by
/// design); what it reads is the settings its components already hold, so nothing is typed in twice and a change to the graph changes what it reads.
/// </para>
/// <para>
/// No component kind is named here. A setting is held only on a component whose kind declares it (the component catalogue states a data set, data store and container
/// setting per kind); the check of story 2 refuses a name on a kind that does not. The ETL tests walk the whole catalogue against this reader, so a kind that starts naming
/// a source fails there until this covers it. A Graph refuses destinations, so everything it names is read.
/// </para>
/// </remarks>
public static class GraphDataSetReads
{
    // The FromKind of a Compute table that is bound to a data set (the other kinds, Stage and Output, name none).
    private const string ComputeTableFromDataSet = "DataSet";

    /// <summary>Lists what a Graph reads, then what it intends to read; each thing once, stored reads first.</summary>
    /// <param name="graph">The Graph data set, read whole (its components and intents).</param>
    /// <returns>The reads, in the order the graph lists its components, then its intents; an intent for something the graph already reads is left out.</returns>
    public static IReadOnlyList<GraphDataSetRead> Of(IGraphDataSetConfiguration graph)
    {
        var reads = new List<GraphDataSetRead>();
        foreach (var component in graph.Components)
        {
            AddComponent(reads, component);
        }

        foreach (var intent in graph.Intents.OrderBy(intent => intent.Ordinal))
        {
            AddIntent(reads, intent);
        }

        return reads;
    }

    private static void AddComponent(List<GraphDataSetRead> reads, GraphDataSetComponentConfiguration component)
    {
        if (!string.IsNullOrWhiteSpace(component.DataSetName))
        {
            Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.DataSet, component.DataSetName, [], false));
        }

        if (!string.IsNullOrWhiteSpace(component.ContainerName) && !string.IsNullOrWhiteSpace(component.DataStoreName))
        {
            Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.Container, component.ContainerName, ParentsOf(component.DataStoreName, component.PathName), false));
        }
        else if (!string.IsNullOrWhiteSpace(component.DataStoreName))
        {
            // A component that names a store and no container (a SQL source) selects from the store.
            Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.DataStore, component.DataStoreName, [], false));
        }

        foreach (var stage in component.Stages)
        {
            AddStage(reads, stage);
        }
    }

    private static void AddStage(List<GraphDataSetRead> reads, GraphDataSetStageConfiguration stage)
    {
        foreach (var lookup in stage.Lookups)
        {
            // A lookup names a data set, or a container of a store (then the data set name is empty).
            if (!string.IsNullOrWhiteSpace(lookup.LookupDataSet))
            {
                Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.DataSet, lookup.LookupDataSet, [], false));
            }

            if (!string.IsNullOrWhiteSpace(lookup.LookupContainerName) && !string.IsNullOrWhiteSpace(lookup.LookupDataStoreName))
            {
                Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.Container, lookup.LookupContainerName, ParentsOf(lookup.LookupDataStoreName, lookup.LookupDataStorePath), false));
            }
        }

        foreach (var table in stage.ComputeTables.Where(table => string.Equals(table.FromKind, ComputeTableFromDataSet, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(table.FromName)))
        {
            Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.DataSet, table.FromName!, [], false));
        }
    }

    // What an intent names comes from its kind's own statement, never from a switch on the kind's name. A kind the collection does not hold names nothing.
    private static void AddIntent(List<GraphDataSetRead> reads, GraphDataSetIntentConfiguration intent)
    {
        var kind = GraphIntentKinds.ByName(intent.IntentKind);
        if (kind.NamesDataStore && !string.IsNullOrWhiteSpace(intent.DataStoreName))
        {
            Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.DataStore, intent.DataStoreName, [], true));
        }

        if (kind.NamesDataSet && !string.IsNullOrWhiteSpace(intent.SourceDataSetName))
        {
            Add(reads, new GraphDataSetRead(GraphDataSetReadKinds.DataSet, intent.SourceDataSetName, [], true) { DataSetId = intent.SourceDataSetId });
        }
    }

    // The store and then the path when one is named; the container sits under both.
    private static string[] ParentsOf(string dataStoreName, string? pathName) =>
        string.IsNullOrWhiteSpace(pathName) ? [dataStoreName] : [dataStoreName, pathName];

    private static void Add(List<GraphDataSetRead> reads, GraphDataSetRead read)
    {
        if (!reads.Any(existing => existing.Names(read)))
        {
            reads.Add(read);
        }
    }
}
