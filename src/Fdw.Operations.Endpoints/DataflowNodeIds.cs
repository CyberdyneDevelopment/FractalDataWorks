using System;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// The id every dataflow graph node carries. Both graph layers build ids here, so an edge in the
/// sources layer joins a node in the base layer by construction rather than by two copies of a format.
/// </summary>
internal static class DataflowNodeIds
{
    /// <summary>Id of a data set node.</summary>
    public static string DataSet(Guid id) => $"dataset_{id}";

    /// <summary>Id of a data set variation node.</summary>
    public static string Variation(Guid id) => $"variation_{id}";

    /// <summary>Id of a pipeline node.</summary>
    public static string Pipeline(string name) => $"pipeline_{name}";

    /// <summary>Id of a datastore node.</summary>
    public static string DataStore(Guid id) => $"datastore_{id}";

    /// <summary>Id of a container node. A container is named within its datastore.</summary>
    public static string Container(string dataStoreName, string containerName) => $"container_{dataStoreName}_{containerName}";

    /// <summary>Id of a data set source node.</summary>
    public static string Source(Guid id) => $"source_{id}";

    /// <summary>Id of a connection node.</summary>
    public static string Connection(string name) => $"connection_{name}";
}
